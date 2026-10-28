// Synthetic audio/API: verifies the isolated page, not online TTS pronunciation.
const { chromium } = require('playwright');
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const html = fs.readFileSync(path.resolve(__dirname, '../../public/voice-preview.html'));
let authenticated = false, delayed = false, browser;
const calls = [];
const server = http.createServer((req, res) => {
  const json = (data, status = 200) => { res.writeHead(status, { 'Content-Type': 'application/json' }); res.end(JSON.stringify(data)); };
  if (req.url === '/voice-preview.html') { res.writeHead(200, { 'Content-Type': 'text/html' }); return res.end(html); }
  if (req.url === '/api/auth/csrf') return json({ token: 'fixture-csrf' });
  if (req.url === '/api/admin/voice-preview') {
    if (!authenticated) return json({}, 401);
    if (req.method === 'GET') return json(['vi','en','zh','ja','ko','fr'].map(code => ({ code, name: code, voices: [{ id: code+'-female', label: 'Nữ' }, { id: code+'-male', label: 'Nam' }] })));
    let body = '';
    req.on('data', chunk => { body += chunk; });
    req.on('end', () => {
      calls.push({ csrf: req.headers['x-csrf-token'], ...JSON.parse(body) });
      const send = () => { if (!res.destroyed) { res.writeHead(200, { 'Content-Type': 'audio/mpeg', 'Cache-Control': 'no-store' }); res.end('synthetic audio'); } };
      if (delayed) setTimeout(send, 500); else send();
    });
    return;
  }
  res.writeHead(404); res.end();
});
(async () => {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  browser = await chromium.launch({ headless: true, executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE || (fs.existsSync('/usr/bin/chromium') ? '/usr/bin/chromium' : undefined), args: ['--no-sandbox'] });
  const page = await browser.newPage({ viewport: { width:390, height:844 } });
  await page.goto(`http://127.0.0.1:${server.address().port}/voice-preview.html`);
  await page.getByRole('alert').filter({ hasText:'Đăng nhập Admin trước' }).waitFor();
  assert.equal(await page.getByRole('button', { name:'Nghe thử', exact:true }).isDisabled(), true);
  authenticated = true; await page.reload();
  await page.waitForFunction(() => !document.getElementById('language').disabled);
  assert.equal(await page.locator('#language option').count(), 6);
  for (const code of ['vi','en','zh','ja','ko','fr']) {
    await page.getByLabel('Ngôn ngữ', { exact:true }).selectOption(code);
    assert.equal(await page.locator('#voice option').count(), 2);
    assert.equal(await page.locator('#text').getAttribute('lang'), code);
  }
  await page.getByLabel('Giọng đọc', { exact:true }).selectOption('fr-male');
  await page.getByLabel('Nội dung nghe thử').fill('Bonjour');
  await page.getByRole('button', { name:'Nghe thử', exact:true }).click();
  await page.waitForFunction(() => !document.getElementById('audio').hidden);
  assert.deepEqual(calls[0], { csrf:'fixture-csrf', text:'Bonjour', language:'fr', voice:'fr-male' });
  await page.getByLabel('Giọng đọc', { exact:true }).selectOption('fr-female');
  assert.equal(await page.locator('#audio').isHidden(), true);
  delayed = true;
  await Promise.all([
    page.waitForResponse(response => response.url().endsWith('/api/auth/csrf')),
    page.getByRole('button', { name:'Nghe thử', exact:true }).click(),
  ]);
  await page.getByRole('button', { name:'Dừng', exact:true }).click();
  await page.waitForTimeout(600); // Outlast the fixture's deliberately delayed response.
  assert.equal(await page.locator('#audio').isHidden(), true);
  assert.equal(await page.getByRole('button', { name:'Nghe thử', exact:true }).isEnabled(), true);
  assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
  console.log('PASS: isolated voice preview login, six languages, two voices, CSRF payload, audio invalidation and cancellation');
})().catch(error => { console.error(error); process.exitCode = 1; }).finally(async () => {
  if (browser) await browser.close();
  server.close();
});
