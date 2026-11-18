import test from 'node:test';
import assert from 'node:assert/strict';
import { lapKeHoachLuuAmThanh } from "../src/utils/chinhSachBoNho.js";

test('fourth language evicts the least recently used whole pack', () => {
  const result = lapKeHoachLuuAmThanh({ a: { language: 'vi', accessedAt: 1 }, b: { language: 'en', accessedAt: 3 }, c: { language: 'ja', accessedAt: 2 } }, { key: 'd', language: 'fr' }, 4);
  assert.deepEqual(result.removed, ['a']);
  assert.deepEqual(Object.keys(result.entries).sort(), ['b', 'c', 'd']);
});
test('a language never exceeds 300 audio files and refreshing a file retains it', () => {
  const entries = Object.fromEntries(Array.from({ length: 300 }, (_, i) => [`file${i}`, { language: 'vi', accessedAt: i }]));
  const result = lapKeHoachLuuAmThanh(entries, { key: 'new', language: 'vi' }, 400);
  assert.equal(Object.keys(result.entries).length, 300);
  assert.deepEqual(result.removed, ['file0']);
  const refreshed = lapKeHoachLuuAmThanh(entries, { key: 'file0', language: 'vi' }, 400);
  assert.deepEqual(refreshed.removed, []);
  assert.equal(refreshed.entries.file0.accessedAt, 400);
});


test('prefetching a fourth language never evicts the active language pack', () => {
  const result = lapKeHoachLuuAmThanh({ a: { language: 'vi', accessedAt: 1 }, b: { language: 'en', accessedAt: 3 }, c: { language: 'ja', accessedAt: 2 } }, { key: 'd', language: 'fr' }, 4, 'vi');
  assert.ok(result.entries.a); assert.ok(result.entries.d); assert.deepEqual(result.removed, ['c']);
});
