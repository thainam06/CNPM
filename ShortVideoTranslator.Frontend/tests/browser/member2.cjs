// Browser regression with synthetic API/GPS/audio fixtures, NOT physical GPS or live TTS.
const { chromium } = require('playwright');
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const out = path.resolve(root, '../artifacts/member2'); fs.mkdirSync(out, { recursive: true });
const hash = data => crypto.createHash('sha256').update(data).digest('hex');
const places = [0.005, 0.02, 0.04, 0.08].map((offset, i) => ({ id: i + 1, name: `Địa điểm kiểm thử ${i + 1}`, category: 'Dữ liệu mô phỏng', description: `Thuyết minh mô phỏng ${i + 1}.`, address: 'Không phải dữ liệu du lịch thật', latitude: 10.77 + offset, longitude: 106.69, locationVerified: true, triggerRadius: 30, isActive: true, imageUrl: '/fixture.png', imageNote: 'Ảnh fixture', isDemo: true }));
const languages = ['vi','en','zh','ja','ko','fr'];
// A decodable 8-second WAV tone tests playback, not pronunciation or real speech.
const samples = 8000 * 8; const audio = Buffer.alloc(44 + samples * 2);
audio.write('RIFF'); audio.writeUInt32LE(audio.length - 8, 4); audio.write('WAVEfmt ', 8); audio.writeUInt32LE(16, 16); audio.writeUInt16LE(1, 20); audio.writeUInt16LE(1, 22); audio.writeUInt32LE(8000, 24); audio.writeUInt32LE(16000, 28); audio.writeUInt16LE(2, 32); audio.writeUInt16LE(16, 34); audio.write('data', 36); audio.writeUInt32LE(samples * 2, 40);
for (let i=0;i<samples;i++) audio.writeInt16LE(Math.round(1000*Math.sin(2*Math.PI*440*i/8000)),44+i*2);
const assets = languages.flatMap(language => places.map(p => { const key=hash(`v1:${language}:${p.description}`); return { key, placeId:p.id, language, text:language==='vi'?p.description:`${language} fixture ${p.id}`, provider:'original', bytes:audio.length, sha256:hash(audio), audioUrl:`/api/narration/audio/${key}` }; }));
const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=', 'base64');
let delayAudio = false; const requests = []; const results = []; let browser;
const server = http.createServer(async(req,res)=> {
  try {
    const url=new URL(req.url,'http://fixture'); const route=url.pathname;
    if(route.startsWith('/api/')) requests.push({method:req.method,path:route});
    const json=(data,status=200)=>{res.writeHead(status,{'Content-Type':'application/json','Cache-Control':'no-store'});res.end(JSON.stringify(data));};
    if(route==='/api/health/ready') return json({status:'ready'});
    if(route==='/api/content') { res.setHeader('ETag','"fixture-1"'); return json({region:'district1',version:1,updatedAt:new Date().toISOString(),syncMode:'full',removedIds:[],items:places.map(place=>({place}))}); }
    if(/^\/api\/places\/\d+\/menu$/.test(route)) return json([]);
    if(/^\/api\/places\/\d+$/.test(route)) return json(places.find(p=>p.id===Number(route.split('/').pop())));
    if(route.startsWith('/api/ui-bundles/')) return json({status:'ready',locale:route.split('/').pop(),sourceHash:'fixture-ui'});
    if(route==='/api/narration/pack-manifest') return json({files:assets.filter(a=>a.language===url.searchParams.get('language'))});
    if(route.startsWith('/api/localizations/')) { let body='';for await(const c of req)body+=c;return json(assets.find(a=>a.placeId===Number(route.split('/').pop())&&a.language===JSON.parse(body).language)); }
    if(route.startsWith('/api/narration/audio/')) {
      const asset=assets.find(a=>a.key===route.split('/').pop());
      if(delayAudio&&asset?.placeId===4) await new Promise(r=>setTimeout(r,1800));
      if(!res.destroyed) {res.writeHead(200,{'Content-Type':'audio/wav'});res.end(audio);}return;
    }
    if(route==='/fixture.png') {res.writeHead(200,{'Content-Type':'image/png'});res.end(png);return;}
    if(route.startsWith('/api/')) return json({message:'Fixture không cung cấp gói bản đồ hoặc dịch vụ thật.'},404);
    const file=path.join(root,'dist',route==='/'?'index.html':route);
    if(!file.startsWith(path.join(root,'dist')+path.sep)) return json({},400);
    const mime={'.html':'text/html','.js':'text/javascript','.css':'text/css','.svg':'image/svg+xml','.webmanifest':'application/manifest+json'};
    if(!fs.existsSync(file)) return json({},404);
    res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream'});fs.createReadStream(file).pipe(res);
  }catch(error){if(!res.headersSent)res.writeHead(500);res.end(String(error));}
});
async function run(name,fn) { await fn();results.push({name,status:'passed'});console.log('PASS',name); }
async function gpsSetup(context, denied=false) {
  await context.addInitScript(({denied})=>{
    let next=0;const watchers=new Map();window.__gpsRequests=0;window.__gpsClears=0;
    const geo={watchPosition(ok,fail){const id=++next;window.__gpsRequests++;watchers.set(id,{ok,fail});queueMicrotask(()=>{if(!watchers.has(id))return;if(denied)fail({code:1});else ok({timestamp:Date.now(),coords:{latitude:10.77,longitude:106.69,accuracy:10}});});return id;},clearWatch(id){window.__gpsClears++;watchers.delete(id);}};
    Object.defineProperty(navigator,'geolocation',{value:geo});
    window.__moveGps=(latitude,longitude=106.69,accuracy=10)=>{for(const w of watchers.values())w.ok({timestamp:Date.now(),coords:{latitude,longitude,accuracy}});};
    window.__gpsError=code=>{for(const w of watchers.values())w.fail({code});};
  },{denied});
}
async function ready(page,base) {
  await page.goto(base); await page.getByRole('button',{name:'Bắt đầu khám phá',exact:true}).click();
  await page.locator('.place-card').first().waitFor();
}
(async()=>{
  await new Promise(r=>server.listen(0,'127.0.0.1',r));const base=`http://127.0.0.1:${server.address().port}`;
  const executablePath=process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE||(fs.existsSync('/usr/bin/chromium')?'/usr/bin/chromium':undefined);
  browser=await chromium.launch({headless:true,executablePath,args:['--no-sandbox']});
  const context=await browser.newContext({viewport:{width:390,height:844},isMobile:true,hasTouch:true});
  // Block only external map tiles. Same-origin production JS/API/SW remain real HTTP requests.
  await context.route('https://**/*',route=>route.abort());await gpsSetup(context);
  const page=await context.newPage();const pageErrors=[];page.on('pageerror',e=>pageErrors.push(e.message));
  await run('GPS tự bắt đầu; bán kính 1/3/5/10 km; không gửi vị trí khi chưa đồng ý',async()=>{
    await ready(page,base);assert.equal(await page.evaluate(()=>window.__gpsRequests),1);
    for(const [radius,count] of [[1000,1],[3000,2],[5000,3],[10000,4]]) {await page.selectOption('#nearby-radius',String(radius));await page.waitForFunction(n=>document.querySelectorAll('.nearby-card').length===n,count);}
    assert.equal(requests.filter(r=>r.path==='/api/runtime/location').length,0);
    await page.locator('.nearby-section').screenshot({path:path.join(out,'gps-nearby-mobile.png')});
  });
  await run('Di chuyển đổi thứ tự; không có địa điểm; GPS kém chính xác',async()=>{
    await page.evaluate(()=>window.__moveGps(10.845));await page.waitForFunction(()=>document.querySelector('.nearby-card h3').textContent.includes('4'));
    await page.evaluate(()=>window.__moveGps(11.5));await page.getByText(/Không có địa điểm đã xác minh/).waitFor();
    await page.evaluate(()=>window.__moveGps(10.77,106.69,200));await page.getByText(/GPS chưa đủ chính xác/).waitFor();
  });
  await run('Dừng GPS ngừng cập nhật; bật lại được',async()=>{
    await page.getByRole('button',{name:'Tắt định vị',exact:true}).click();await page.evaluate(()=>window.__moveGps(10.77));
    assert.equal(await page.locator('.nearby-card').count(),0);assert.ok(await page.evaluate(()=>window.__gpsClears)>0);
    await page.getByRole('button',{name:'Bật định vị & thuyết minh gần bạn',exact:true}).click();await page.locator('.nearby-card').first().waitFor();
    assert.equal(await page.evaluate(()=>window.__gpsRequests),2);
    await page.getByRole('button',{name:'Dừng định vị',exact:true}).click();
    await page.getByRole('button',{name:'Bật định vị & thuyết minh gần bạn',exact:true}).waitFor();
    assert.equal(await page.locator('.nearby-card').count(),0);
    await page.getByRole('button',{name:'Thử lại định vị',exact:true}).click();await page.locator('.nearby-card').first().waitFor();
    assert.equal(await page.evaluate(()=>window.__gpsRequests),3);
  });
  await run('Responsive 320/390/768/1280 px, không tràn ngang',async()=>{
    for(const width of [320,390,768,1280]) {await page.setViewportSize({width,height:844});await page.waitForTimeout(150);assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),`overflow ${width}px`);}
    await page.setViewportSize({width:390,height:844});await page.screenshot({path:path.join(out,'mobile-page.png'),fullPage:true});
  });
  await run('Nghe, dừng, phát lại; đổi địa điểm dừng audio',async()=>{
    const listen=page.locator('.narration-panel .listen-button');await listen.click();await page.waitForFunction(()=>{const a=document.querySelector('audio');return a&&!a.paused&&a.readyState>=2;});
    await listen.click();assert.equal(await page.locator('audio').evaluate(a=>a.paused),true);
    await listen.click();await page.waitForFunction(()=>!document.querySelector('audio').paused);
    await page.locator('.place-card').nth(1).click();await page.waitForFunction(()=>!document.querySelector('audio'));
  });
  await run('Đổi ngôn ngữ; nghe bản fixture tiếng Anh',async()=>{
    await page.selectOption('#language','en');await page.locator('.narration-panel .listen-button').click();
    await page.waitForFunction(()=>{const a=document.querySelector('audio');return a&&!a.paused&&a.readyState>=2;});
    await page.selectOption('#language','vi');await page.waitForFunction(()=>!document.querySelector('audio'));
    assert.equal(await page.evaluate(()=>window.__gpsRequests),3, 'Changing narration language must not create another GPS watcher');
  });
  await run('Đổi địa điểm trong lúc tải không phát audio cũ',async()=>{
    delayAudio=true;await page.locator('.place-card').nth(3).click();
    await page.locator('.narration-panel .listen-button').click();
    await page.locator('.place-card').first().click();await page.waitForTimeout(2200);
    assert.equal(await page.locator('audio').count(),0);delayAudio=false;
  });
  await run('Offline thật trong browser: reload shell, catalog, ảnh và audio đã lưu',async()=>{
    await page.evaluate(()=>navigator.serviceWorker.ready);await page.waitForFunction(()=>!!navigator.serviceWorker.controller);
    await page.waitForFunction(()=>[...document.querySelectorAll('.pwa-ready')].some(x=>x.textContent.includes('Giao diện')));
    // Cache initial narration and photo using the normal application flow.
    await page.locator('.narration-panel .listen-button').click();await page.waitForFunction(()=>document.querySelector('audio')?.readyState>=2);
    await context.setOffline(true);await page.reload();await page.locator('.place-card').first().waitFor();
    await page.waitForFunction(()=>document.querySelector('.narration-panel .listen-button')&&!document.querySelector('.narration-panel .listen-button').disabled);
    await page.locator('.narration-panel .listen-button').click();await page.waitForFunction(()=>{const a=document.querySelector('audio');return a&&!a.paused&&a.readyState>=2;});
    assert.ok(await page.locator('.place-card img').first().evaluate(img=>img.complete&&img.naturalWidth>0));
    await page.screenshot({path:path.join(out,'offline-mobile.png'),fullPage:true});await context.setOffline(false);
  });
  await run('Thiếu PMTiles: tải gói đầy đủ báo lỗi, không báo đã hoàn tất',async()=>{
    await page.getByRole('button',{name:'Tải gói offline đầy đủ',exact:true}).click();
    await page.locator('.offline-bundle-tools').getByText(/Fixture không cung cấp/).waitFor();
    assert.equal(await page.locator('.offline-bundle-tools .pwa-ready').count(),0);
  });
  const denied=await browser.newContext({viewport:{width:390,height:844}});await denied.route('https://**/*',r=>r.abort());await gpsSetup(denied,true);
  await run('Từ chối GPS vẫn xem được địa điểm thủ công',async()=>{
    const p=await denied.newPage();await ready(p,base);await p.getByText(/Quyền vị trí bị từ chối/).waitFor();
    await p.locator('.place-card').nth(1).click();await p.locator('#chi-tiet').getByRole('heading',{name:'Địa điểm kiểm thử 2',exact:true}).waitFor();
    await p.locator('.nearby-section').screenshot({path:path.join(out,'gps-denied.png')});await denied.close();
  });
  assert.deepEqual(pageErrors,[]);await context.close();
})().catch(error=>{results.push({name:'Browser regression',status:'failed',error:error.stack});console.error(error);process.exitCode=1;}).finally(async()=>{
  fs.writeFileSync(path.join(out,'results.json'),JSON.stringify({environment:'Chromium with synthetic API/GPS/WAV fixtures; no physical phone, live TTS or full PMTiles pack validation',results},null,2));
  if(browser)await browser.close();server.close();
});
