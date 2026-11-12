import test from 'node:test';
import assert from 'node:assert/strict';
import { chuQuanTri, ngonNguQuanTri, tuDienQuanTri } from "../src/tuDienQuanTri.js";
test('portal dictionaries have the same complete key set and preserve source content', () => {
  const keys = Object.keys(tuDienQuanTri.vi);
  for (const language of ngonNguQuanTri) {
    assert.deepEqual(Object.keys(tuDienQuanTri[language]), keys);
    assert.ok(Object.values(tuDienQuanTri[language]).every(value => typeof value === 'string' && value.trim()));
    assert.equal(chuQuanTri(language, 'Chợ Bến Thành'), 'Chợ Bến Thành');
    assert.equal(chuQuanTri(language, 'poi:update'), 'poi:update');
  }
  assert.equal(chuQuanTri('en', 'Tên đăng nhập'), 'Username');
  assert.equal(chuQuanTri('ja', 'Địa điểm'), 'スポット');
});
