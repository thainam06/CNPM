import test from 'node:test';
import assert from 'node:assert/strict';
import { portalText, portalLanguages, portalMessages } from '../src/portalMessages.js';
test('portal dictionaries have the same complete key set and preserve source content', () => {
  const keys = Object.keys(portalMessages.vi);
  for (const language of portalLanguages) {
    assert.deepEqual(Object.keys(portalMessages[language]), keys);
    assert.ok(Object.values(portalMessages[language]).every(value => typeof value === 'string' && value.trim()));
    assert.equal(portalText(language, 'Chợ Bến Thành'), 'Chợ Bến Thành');
    assert.equal(portalText(language, 'poi:update'), 'poi:update');
  }
  assert.equal(portalText('en', 'Tên đăng nhập'), 'Username');
  assert.equal(portalText('ja', 'Địa điểm'), 'スポット');
});
