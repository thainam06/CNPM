import { test } from 'node:test';
import assert from 'node:assert/strict';
import { newDeviceId } from '../src/deviceId.js';
test('HTTP LAN fallback creates a UUID v4 using secure random bytes', () => {
  const source = { getRandomValues: array => crypto.getRandomValues(array) };
  const ids = Array.from({ length: 100 }, () => newDeviceId(source));
  assert.equal(new Set(ids).size, 100);
  for (const id of ids) assert.match(id, /^[a-f\d]{8}-[a-f\d]{4}-4[a-f\d]{3}-[89ab][a-f\d]{3}-[a-f\d]{12}$/);
});
