import { narrationKey } from './narrationKey.js';
import { placePhotos } from './placePhotos.js';
export const bundleKey = language => `offline-bundle:district1:${language}`;
const abort = signal => { if (signal?.aborted) throw new DOMException('Đã hủy tải gói.', 'AbortError'); };
export async function sha256(blob) {
  const bytes = blob instanceof Blob ? await blob.arrayBuffer() : blob;
  return Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', bytes)), b => b.toString(16).padStart(2, '0')).join('');
}
async function verifiedBlob(response, asset, signal) {
  const blob = await response.blob(); abort(signal);
  if (blob.size !== asset.bytes || await sha256(blob) !== asset.sha256) throw new Error(`Tài nguyên ${asset.audioUrl || asset.url || ''} không khớp checksum (${blob.size}/${asset.bytes} byte); gói cũ được giữ.`);
  return blob;
}
export async function stageBundle({ language, request, fetchPhoto, signal, progress = () => {} }) {
  if (!['vi', 'en', 'zh', 'ja', 'ko', 'fr'].includes(language)) throw new Error('Ngôn ngữ không hỗ trợ.');
  abort(signal); progress('Đang lấy danh sách địa điểm và giao diện…');
  const contentResponse = await request(`/content?language=${language}`, { signal });
  const snapshot = await contentResponse.json();
  const etag = contentResponse.headers.get('ETag');
  if (!etag || snapshot.region !== 'district1' || snapshot.syncMode !== 'full' || !snapshot.items.length) throw new Error('Danh sách Quận 1 chưa sẵn sàng.');
  const places = snapshot.items.map(item => item.place);
  if (places.length > 300) throw new Error('Demo hỗ trợ tối đa 300 địa điểm trong một gói.');
  const ui = await (await request(`/ui-bundles/${language}`, { signal })).json();
  if (ui.status !== 'ready' || ui.locale !== language) throw new Error('Giao diện đúng ngôn ngữ chưa sẵn sàng.');
  progress('Đang tải và xác minh bản đồ…');
  const mapResponse = await request('/maps/pack-manifest', { signal });
  const manifest = await mapResponse.json(); const mapEtag = mapResponse.headers.get('ETag');
  if (!mapEtag || manifest.scope !== 'district1' || manifest.bytes > 100 * 1024 * 1024) throw new Error('Gói bản đồ Quận 1 không hợp lệ.');
  const mapBlob = await verifiedBlob(await request(manifest.url.replace(/^\/api/, ''), { signal, cache: 'no-store' }), manifest, signal);
  const mapHeader = new Uint8Array(await mapBlob.slice(0, 8).arrayBuffer());
  if (new TextDecoder().decode(mapHeader.slice(0, 7)) !== 'PMTiles' || mapHeader[7] !== 3) throw new Error('Bản đồ không phải PMTiles v3.');
  const entries = [['places:district1', places], ['catalog-sync:district1', { updatedAt: snapshot.updatedAt, etag }],
    ['map-pack:district1', { blob: mapBlob, manifest, downloadedAt: Date.now() }], [`ui-bundle:${language}`, ui]];
  const photos = []; let imageBytes = 0;
  const urls = [...new Set(places.flatMap(p => placePhotos(p).map(photo => photo.url)))];
  for (const [index, url] of urls.entries()) {
    progress(`Đang tải ảnh: ${index + 1}/${urls.length}`); abort(signal);
    const response = await fetchPhoto(url, signal);
    if (!response.ok || response.type === 'opaque') throw new Error('Không tải được ảnh để lưu offline. Kiểm tra mạng/quyền CORS của nguồn ảnh.');
    const blob = await response.blob(); imageBytes += blob.size;
    const head = new Uint8Array(await blob.slice(0, 12).arrayBuffer());
    const signature = (head[0] === 255 && head[1] === 216 && head[2] === 255) ||
      (head[0] === 137 && head[1] === 80 && head[2] === 78 && head[3] === 71) ||
      (new TextDecoder().decode(head.slice(0, 4)) === 'RIFF' && new TextDecoder().decode(head.slice(8, 12)) === 'WEBP');
    if (!signature || blob.size > 8 * 1024 * 1024 || imageBytes > 60 * 1024 * 1024) throw new Error('Ảnh sai định dạng hoặc vượt dung lượng cho phép.');
    const photo = { url, blob, sha256: await sha256(blob) }; photos.push({ url, sha256: photo.sha256 }); entries.push([`photo:${url}`, photo]);
  }
  const audio = [];
  const audioManifest = await (await request(`/narration/pack-manifest?language=${language}`, { signal })).json();
  const expected = [...new Set(await Promise.all(places.map(p => narrationKey(p.description, language, p.narrationRevisions?.[language]))))];
  for (const [index, key] of expected.entries()) {
    const asset = audioManifest.files.find(f => f.key === key && f.language === language);
    if (!asset || asset.bytes > 10 * 1024 * 1024) throw new Error('Còn thiếu MP3 đúng nội dung/ngôn ngữ. Admin cần tạo audio trước khi tải gói đầy đủ.');
    progress(`Đang tải và xác minh MP3: ${index + 1}/${expected.length}`);
    const blob = await verifiedBlob(await request(asset.audioUrl.replace(/^\/api/, ''), { signal, cache: 'no-store' }), asset, signal);
    entries.push([`audio:${key}`, blob], [`translation:${key}`, { text: asset.text, language, provider: asset.provider || (language === 'vi' ? 'original' : 'MyMemory'), isMachineTranslation: language !== 'vi' && asset.provider !== 'reviewed' }]);
    audio.push({ key, sha256: asset.sha256, language });
  }
  progress('Đang kiểm tra phiên bản trước khi kích hoạt…');
  const current = await request(`/content?language=${language}`, { signal, headers: { 'If-None-Match': etag } });
  const currentMap = await request('/maps/pack-manifest', { signal, headers: { 'If-None-Match': mapEtag } });
  if (current.status !== 304 || currentMap.status !== 304) throw new Error('Nội dung hoặc bản đồ vừa thay đổi. Tải lại; gói cũ chưa bị thay thế.');
  abort(signal);
  const meta = { language, region: 'district1', version: snapshot.version, installedAt: Date.now(), places,
    photos, audio, uiSourceHash: ui.sourceHash, mapFingerprint: manifest.fingerprint, mapHash: manifest.sha256,
    bytes: mapBlob.size + imageBytes + entries.filter(([key]) => key.startsWith('audio:')).reduce((n, [, blob]) => n + blob.size, 0) };
  entries.push([bundleKey(language), meta]);
  return { entries, audio, meta };
}
export async function installBundle(options, commit) {
  const staged = await stageBundle(options); abort(options.signal);
  await commit(staged, options.signal); return staged.meta;
}

// Reconcile stored pieces instead of trusting an old "ready" label after eviction/deletion.
export async function inspectBundle(language, read) {
  const meta = await read(bundleKey(language));
  if (!meta) return { status: 'missing', message: 'Chưa có gói đầy đủ trên thiết bị.' };
  const missing = [];
  const places = await read('places:district1');
  if (!places || JSON.stringify(places) !== JSON.stringify(meta.places)) missing.push('danh sách địa điểm');
  const ui = await read(`ui-bundle:${language}`);
  if (ui?.status !== 'ready' || ui.sourceHash !== meta.uiSourceHash) missing.push('giao diện');
  const map = await read('map-pack:district1');
  if (!map?.blob || map.manifest?.fingerprint !== meta.mapFingerprint || await sha256(map.blob) !== meta.mapHash) missing.push('bản đồ');
  for (const photo of meta.photos) {
    const saved = await read(`photo:${photo.url}`);
    if (!saved?.blob || await sha256(saved.blob) !== photo.sha256) missing.push('ảnh');
  }
  for (const asset of meta.audio) {
    const saved = await read(`audio:${asset.key}`); const text = await read(`translation:${asset.key}`);
    if (!saved || !text?.text || text.language !== language || await sha256(saved) !== asset.sha256) missing.push('audio/bản dịch');
  }
  return { status: missing.length ? 'repair' : 'ready', meta, missing: [...new Set(missing)] };
}
