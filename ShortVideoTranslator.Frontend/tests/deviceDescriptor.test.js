import { test } from 'node:test';
import assert from 'node:assert/strict';
import { deviceDescriptor } from '../src/deviceDescriptor.js';
test('device description distinguishes browsers without uploading the raw user agent', () => {
  assert.deepEqual(deviceDescriptor('Windows NT Chrome/120 Safari/537 Edg/120'), { browser: 'Edge', operatingSystem: 'Windows', type: 'Desktop' });
  assert.deepEqual(deviceDescriptor('Android 14 Mobile Chrome/120 Safari/537'), { browser: 'Chrome', operatingSystem: 'Android', type: 'Mobile' });
  assert.equal(deviceDescriptor('iPad Safari/600').type, 'Tablet');
  assert.equal(deviceDescriptor('Windows Chrome/120 OPR/120').browser, 'Opera');
  assert.equal(deviceDescriptor('').type, 'Unknown');
});
