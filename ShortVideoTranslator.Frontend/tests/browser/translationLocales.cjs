// Real React UI with synthetic Admin responses; never calls live translation/TTS.
const { chromium } = require('playwright');
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const source = 'Nội dung tiếng Việt gốc không được thay đổi.';
const draftText = 'An existing English translation.';
const writes = [];
const place = { id: 1, name: 'Chợ Bến Thành', description: source, isActive: true };
const server = http.createServer((req, res) => {
  const route = new URL(req.url, 'http://fixture').pathname;
  const json = data => { res.writeHead(200, { 'Content-Type': 'application/json' }); res.end(JSON.stringify(data)); };
  if (route === '/api/auth/csrf') return json({ token: 'fixture-csrf' });
  if (route === '/api/auth/me') return json({ id: 'fixture-admin', displayName: 'Fixture Admin', role: 'super_admin', permissions: ['poi:update', 'content:publish', 'content:moderate'] });
  if (route === '/api/admin/places') return json([]);
  if (route === '/api/admin/translations') return json({ places: [place] });
  if (/^\/api\/admin\/translations\/1\/[a-z]+$/.test(route)) {
    if (req.method === 'PUT') {
      let body = '';
      req.on('data', chunk => { body += chunk; });
      req.on('end', () => { writes.push({ route, ...JSON.parse(body) }); json({}); });
      return;
    }
    return json({ place, sourceHash: 'fixture-source', draft: { text: draftText, sourceHash: 'fixture-source', status: 'draft', version: 1 } });
  }
  if (route.startsWith('/api/')) { res.writeHead(404); return res.end('{}'); }
  const file = path.resolve(root, 'dist', route === '/' ? 'index.html' : '.' + route);
  const target = file.startsWith(path.join(root, 'dist') + path.sep) && fs.existsSync(file) && fs.statSync(file).isFile() ? file : path.join(root, 'dist/index.html');
  const mime = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css' };
  res.writeHead(200, { 'Content-Type': mime[path.extname(target)] || 'application/octet-stream' });
  fs.createReadStream(target).pipe(res);
});
let browser;
(async () => {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  browser = await chromium.launch({ headless: true, executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE || (fs.existsSync('/usr/bin/chromium') ? '/usr/bin/chromium' : undefined), args: ['--no-sandbox'] });
  const page = await browser.newPage();
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.route('https://**/*', route => route.abort());
  await page.goto(`http://127.0.0.1:${server.address().port}/admin`);
  await page.getByRole('button', { name: 'Bản dịch thuyết minh', exact: true }).click();
  const panel = page.locator('.portal-card').filter({ has: page.locator('textarea[readonly]') });
  const cases = [
    ['vi', 'Biên tập và duyệt bản dịch thuyết minh', 'Lưu bản nháp', 'Duyệt & tạo MP3'],
    ['en', 'Edit and approve narration translations', 'Save draft', 'Approve & generate MP3'],
    ['zh', '编辑并审核讲解译文', '保存草稿', '审核通过并生成 MP3'],
    ['ja', '解説の翻訳を編集・承認', '下書きを保存', '承認してMP3を生成'],
    ['ko', '해설 번역 편집 및 승인', '초안 저장', '승인 및 MP3 생성'],
    ['fr', 'Modifier et approuver les traductions de narration', 'Enregistrer le brouillon', 'Approuver et générer le MP3'],
  ];
  for (const [language, heading, save, approve] of cases) {
    await page.getByRole('combobox', { name: 'Portal language' }).selectOption(language);
    await panel.getByRole('heading', { name: heading, exact: true }).waitFor();
    await panel.getByRole('button', { name: save, exact: true }).waitFor();
    await panel.getByRole('button', { name: approve, exact: true }).waitFor();
    assert.equal(await panel.locator('textarea[readonly]').inputValue(), source);
    assert.equal(await panel.locator('textarea[lang]').inputValue(), draftText);
    assert.equal(await panel.locator('textarea[lang]').getAttribute('lang'), 'en');
  }
  assert.deepEqual(writes, [], 'Changing UI language must not save or translate narration');
  await panel.locator('textarea[lang]').fill('An edited English translation.');
  await Promise.all([
    page.waitForResponse(response => response.url().endsWith('/api/admin/translations') && response.request().method() === 'GET'),
    panel.getByRole('button', { name: 'Enregistrer le brouillon', exact: true }).click(),
  ]);
  assert.deepEqual(writes, [{ route: '/api/admin/translations/1/en', text: 'An edited English translation.', sourceHash: 'fixture-source', expectedVersion: 1 }]);
  assert.deepEqual(errors, []);
  console.log('PASS: translation editor labels in six UI languages; source, target language and draft save remain unchanged');
})().catch(error => { console.error(error); process.exitCode = 1; }).finally(async () => {
  if (browser) await browser.close();
  server.close();
});
