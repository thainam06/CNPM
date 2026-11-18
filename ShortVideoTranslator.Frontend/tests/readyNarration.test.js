import test from 'node:test';
import assert from 'node:assert/strict';
import { timThuyetMinhSanSang, xacMinhAmThanh } from "../src/services/thuyetMinhSanSang.js";
import { taoKhoaThuyetMinh } from "../src/utils/khoaThuyetMinh.js";

const response = files => async () => ({ json: async () => ({ files }) });
async function assetFor(text = 'Source', language = 'en', revision = '') {
  const key = await taoKhoaThuyetMinh(text, language, revision);
  const blob = new Blob(['Synthetic integrity fixture, not real MP3.']);
  const digest = await crypto.subtle.digest('SHA-256', await blob.arrayBuffer());
  const tinhSha256 = Buffer.from(digest).toString('hex');
  return { blob, asset: { key, text:'Translated fixture', language, audioUrl:`/api/narration/audio/${key}`, bytes:blob.size, sha256: tinhSha256, provider:'reviewed' } };
}
test('prepared narration never substitutes another source, language or published revision', async () => {
  const { asset } = await assetFor();
  assert.deepEqual(await timThuyetMinhSanSang('Source','en','',response([asset])), asset);
  for (const [text, language, revision] of [['Edited source','en',''], ['Source','fr',''], ['Source','en','new-publication']]) {
    assert.equal(await timThuyetMinhSanSang(text,language,revision,response([asset])), null);
  }
  assert.equal(await timThuyetMinhSanSang('Source','en','',response([{...asset, audioUrl:'https://other.example/audio'}])), null);
  assert.equal(await timThuyetMinhSanSang('Source','en','',response([{...asset, language:'vi'}])), null);
});
test('prepared audio is verified before caching; same-length corruption and truncation fail', async () => {
  const { blob, asset } = await assetFor();
  assert.equal(await xacMinhAmThanh(blob,asset),blob);
  await assert.rejects(xacMinhAmThanh(new Blob(['x'.repeat(blob.size)]),asset),/MP3/);
  await assert.rejects(xacMinhAmThanh(blob.slice(1),asset),/MP3/);
});
test('manifest failure retains on-demand path but cancellation cannot continue', async () => {
  assert.equal(await timThuyetMinhSanSang('Source','en','',async()=>{throw new Error('Offline');}),null);
  assert.equal(await timThuyetMinhSanSang('Source','en','',response({malformed:true})),null);
  const controller = new AbortController(); controller.abort(); let requests = 0;
  await assert.rejects(timThuyetMinhSanSang('Source','en','',async()=>{requests++;},controller.signal), {name:'AbortError'});
  assert.equal(requests,0);
});
