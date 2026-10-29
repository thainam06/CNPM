import { test } from 'node:test';
import assert from 'node:assert/strict';
import { presenceDeviceId } from '../src/presenceIdentity.js';
const storage = () => { const entries = new Map(); return { getItem: key => entries.get(key), setItem: (key, value) => entries.set(key, value) }; };
test('tabs sharing storage have one presence ID; separate browsers differ', () => {
  const browser = storage();
  assert.equal(presenceDeviceId(browser), presenceDeviceId(browser));
  assert.notEqual(presenceDeviceId(browser), presenceDeviceId(storage()));
  assert.equal(browser.getItem('smarttour-consent'), undefined);
  assert.equal(browser.getItem('smarttour-device'), undefined);
});
test('restricted localStorage does not prevent page startup or rotate ID every heartbeat', () => {
  const blocked = { getItem() { throw new Error('denied'); } };
  assert.equal(presenceDeviceId(blocked), presenceDeviceId(blocked));
  const invalid = storage(); invalid.setItem('smarttour-presence-device', 'bad-id');
  assert.match(presenceDeviceId(invalid), /^[a-f\d-]{36}$/);
});
