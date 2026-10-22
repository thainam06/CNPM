import test from 'node:test';
import assert from 'node:assert/strict';
import { installBundle, inspectBundle, sha256, bundleKey } from '../src/bundleService.js';
import { narrationKey } from '../src/narrationKey.js';

// Synthetic byte fixtures test checksums/activation; they are not real MP3/PMTiles.
async function fixture(overrides = {}) {
  const text = 'Nguồn demo';
  const language = overrides.reviewed ? 'en' : 'vi';
  const revision = overrides.reviewed ? 'review-1' : '';
  const key = await narrationKey(text, language, revision);
  const map = new Blob([new TextEncoder().encode('PMTiles'), new Uint8Array([3, 1, 2])]);
  const audio = new Blob([new Uint8Array([11, 22, 33])]);
  const photo = new Blob([new Uint8Array([255, 216, 255, 1])], { type: 'image/jpeg' });
  const manifest = { scope: 'district1', fingerprint: 'map-v1', bytes: map.size, sha256: await sha256(map), url: '/api/maps/files/district1.pmtiles' };
  const asset = { key, text: overrides.reviewed ? 'Reviewed fixture.' : text, language, provider: overrides.reviewed ? 'reviewed' : 'original', bytes: audio.size, sha256: await sha256(audio), audioUrl: '/api/narration/audio/' + key };
  const places = [{ id: 4, region: 'district1', name: 'Test', description: text, narrationRevisions: revision ? { en: revision } : null, imageUrl: 'https://example.com/test.jpg', images: overrides.gallery ? [{ url: 'https://example.com/extra.jpg' }] : [] }];
  const store = new Map([['old-pack', 'unchanged']]); let commits = 0;
  const json = (value, etag) => new Response(JSON.stringify(value), { headers: { 'Content-Type': 'application/json', ...(etag ? { ETag: etag } : {}) } });
  const request = async (path, options = {}) => {
    if (overrides.staleHttpCache && (path.startsWith('/maps/files') || path.startsWith('/narration/audio')) && options.cache !== 'no-store') return new Response(new Blob(['old HTTP cache bytes']));
    if (options.headers?.['If-None-Match']) return overrides.changed ? json({ version: 2 }, 'new') : new Response(null, { status: 304 });
    if (path.startsWith('/content')) return json({ region: 'district1', syncMode: 'full', version: 1, updatedAt: '2026-10-14', items: places.map(place => ({ place })) }, 'catalog-v1');
    if (path.startsWith('/ui-bundles')) return json({ status: 'ready', locale: language, sourceHash: 'ui-v1' });
    if (path === '/maps/pack-manifest') return json(manifest, 'map-v1');
    if (path.startsWith('/maps/files')) return new Response(map);
    if (path.startsWith('/narration/pack-manifest')) return json({ files: overrides.missingAudio ? [] : [asset] });
    if (path.startsWith('/narration/audio')) return new Response(overrides.corruptAudio ? new Blob(['bad']) : audio);
    throw new Error('Unexpected fixture request: ' + path);
  };
  const options = { language, request, signal: overrides.signal, fetchPhoto: async () => overrides.photoError ? new Response('blocked', { status: 403 }) : new Response(photo) };
  const commit = async staged => { commits++; for (const [key, value] of staged.entries) store.set(key, value); };
  return { options, commit, store, key, get commits() { return commits; } };
}
test('complete bundle activates once with matching snapshot, photos, audio and map', async () => {
  const f = await fixture(); await installBundle(f.options, f.commit);
  assert.equal(f.commits, 1); assert.equal((await inspectBundle('vi', key => f.store.get(key))).status, 'ready');
  assert.equal(f.store.get(bundleKey('vi')).photos.length, 1);
});
test('bundle bypasses stale HTTP media cache and validates current manifest bytes', async () => {
  const f = await fixture({ staleHttpCache: true });
  await installBundle(f.options, f.commit);
  assert.equal((await inspectBundle('vi', key => f.store.get(key))).status, 'ready');
});
test('Reviewed bundle selects the published revision and preserves human review provenance offline', async () => {
  const f = await fixture({ reviewed: true }); await installBundle(f.options, f.commit);
  const translation = f.store.get('translation:' + f.key);
  assert.equal(translation.text, 'Reviewed fixture.'); assert.equal(translation.provider, 'reviewed');
  assert.equal(translation.isMachineTranslation, false);
  assert.equal((await inspectBundle('en', key => f.store.get(key))).status, 'ready');
});
test('Complete bundle includes additional gallery photos with the cover', async () => {
  const f = await fixture({ gallery: true }); await installBundle(f.options, f.commit);
  assert.equal(f.store.get(bundleKey('vi')).photos.length, 2);
  assert.ok(f.store.get('photo:https://example.com/extra.jpg').blob instanceof Blob);
});
for (const [name, flag] of [['corrupt audio', 'corruptAudio'], ['changed catalog/map', 'changed'], ['missing correct-language audio', 'missingAudio'], ['image provider failure', 'photoError']]) {
  test(name + ' cannot replace the previous bundle', async () => {
    const f = await fixture({ [flag]: true });
    await assert.rejects(installBundle(f.options, f.commit)); assert.equal(f.commits, 0);
    assert.deepEqual([...f.store.entries()], [['old-pack', 'unchanged']]);
  });
}
test('abort before activation leaves previous bundle intact', async () => {
  const controller = new AbortController(); const f = await fixture({ signal: controller.signal });
  f.options.progress = message => { if (message.includes('phiên bản')) controller.abort(); };
  await assert.rejects(installBundle(f.options, f.commit), { name: 'AbortError' }); assert.equal(f.commits, 0);
});
test('LRU removal or same-length corruption makes stored bundle repair-required', async () => {
  const f = await fixture(); await installBundle(f.options, f.commit);
  f.store.delete('audio:' + f.key);
  assert.equal((await inspectBundle('vi', key => f.store.get(key))).status, 'repair');
  f.store.set('audio:' + f.key, new Blob([new Uint8Array([11, 22, 34])]));
  assert.equal((await inspectBundle('vi', key => f.store.get(key))).status, 'repair');
});
