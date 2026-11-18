import { test } from 'node:test';
import assert from 'node:assert/strict';
import { layMaTrinhDuyet } from "../src/utils/dinhDanhTruyCap.js";
const storage = () => { const entries = new Map(); return { getItem: key => entries.get(key), setItem: (key, value) => entries.set(key, value) }; };
test('tabs sharing storage have one presence ID; separate browsers differ', () => {
  const browser = storage();
  assert.equal(layMaTrinhDuyet(browser), layMaTrinhDuyet(browser));
  assert.notEqual(layMaTrinhDuyet(browser), layMaTrinhDuyet(storage()));
  assert.equal(browser.getItem('smarttour-consent'), undefined);
  assert.equal(browser.getItem('smarttour-device'), undefined);
});
test('restricted localStorage does not prevent page startup or rotate ID every heartbeat', () => {
  const blocked = { getItem() { throw new Error('denied'); } };
  assert.equal(layMaTrinhDuyet(blocked), layMaTrinhDuyet(blocked));
  const invalid = storage(); invalid.setItem('smarttour-presence-device', 'bad-id');
  assert.match(layMaTrinhDuyet(invalid), /^[a-f\d-]{36}$/);
});
