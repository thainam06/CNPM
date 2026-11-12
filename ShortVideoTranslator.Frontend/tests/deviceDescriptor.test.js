import { test } from 'node:test';
import assert from 'node:assert/strict';
import { moTaThietBi } from "../src/moTaThietBi.js";
test('device description distinguishes browsers without uploading the raw user agent', () => {
  assert.deepEqual(moTaThietBi('Windows NT Chrome/120 Safari/537 Edg/120'), { browser: 'Edge', operatingSystem: 'Windows', type: 'Desktop' });
  assert.deepEqual(moTaThietBi('Android 14 Mobile Chrome/120 Safari/537'), { browser: 'Chrome', operatingSystem: 'Android', type: 'Mobile' });
  assert.equal(moTaThietBi('iPad Safari/600').type, 'Tablet');
  assert.equal(moTaThietBi('Windows Chrome/120 OPR/120').browser, 'Opera');
  assert.equal(moTaThietBi('').type, 'Unknown');
});
