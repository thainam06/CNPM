import test from 'node:test';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { taoKhoaThuyetMinh } from "../src/utils/khoaThuyetMinh.js";

test('Existing automatic audio keeps its key; reviewed revisions use a separate namespace', async () => {
  const source = 'Nội dung Quận 1';
  const legacy = createHash('sha256').update(`v1:en:${source}`).digest('hex');
  assert.equal(await taoKhoaThuyetMinh(source, 'en'), legacy);
  const approved = await taoKhoaThuyetMinh(source, 'en', 'revision-1');
  assert.notEqual(approved, legacy);
  assert.notEqual(approved, await taoKhoaThuyetMinh(source, 'en', 'revision-2'));
  assert.notEqual(approved, await taoKhoaThuyetMinh(source, 'fr', 'revision-1'));
  assert.notEqual(approved, await taoKhoaThuyetMinh(`revision-1:${source}`, 'en'));
  assert.equal(approved, createHash('sha256').update(`v2:en:revision-1:${source}`).digest('hex'));
});
