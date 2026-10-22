import { apiRequest } from './api';
import { offlineGet, offlineSet, offlineAssetsChanged } from './offline';

export const mapPackKey = 'map-pack:district1';
export async function getMapPack() { return offlineGet(mapPackKey); }
export async function downloadMapPack(signal, progress) {
  const manifest = await (await apiRequest('/maps/pack-manifest', { signal })).json();
  if (manifest.bytes > 100 * 1024 * 1024) throw new Error('Gói bản đồ vượt giới hạn 100 MB của demo.');
  progress('Đang tải gói bản đồ Quận 1…');
  const blob = await (await apiRequest(manifest.url.replace(/^\/api/, ''), { signal, cache: 'no-store' })).blob();
  const bytes = await blob.arrayBuffer();
  const hash = Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', bytes)), byte => byte.toString(16).padStart(2, '0')).join('');
  if (hash !== manifest.sha256 || blob.size !== manifest.bytes) throw new Error('Gói bản đồ không khớp checksum; bản đã lưu trước vẫn được giữ.');
  if (new TextDecoder().decode(bytes.slice(0, 7)) !== 'PMTiles' || new Uint8Array(bytes)[7] !== 3) throw new Error('Gói không phải PMTiles v3 hợp lệ.');
  if (signal?.aborted) throw new DOMException('Đã hủy tải bản đồ', 'AbortError');
  // One transaction activates blob + manifest together; failed downloads cannot replace a working pack.
  const pack = { blob, manifest, downloadedAt: Date.now() };
  await offlineSet(mapPackKey, pack);
  offlineAssetsChanged();
  return pack;
}
export async function removeMapPack() { await offlineSet(mapPackKey, null); offlineAssetsChanged(); }

export function offlineMapStyle(sourceUrl) {
  return { version: 8, sources: { basemap: { type: 'vector', url: sourceUrl, attribution: '© OpenStreetMap contributors · Protomaps' } }, layers: [
    { id: 'background', type: 'background', paint: { 'background-color': '#f7f1e8' } },
    { id: 'earth', type: 'fill', source: 'basemap', 'source-layer': 'earth', paint: { 'fill-color': '#f2eadc' } },
    { id: 'landuse', type: 'fill', source: 'basemap', 'source-layer': 'landuse', paint: { 'fill-color': '#d8dfc2', 'fill-opacity': .65 } },
    { id: 'water', type: 'fill', source: 'basemap', 'source-layer': 'water', paint: { 'fill-color': '#b4d2dc' } },
    { id: 'buildings', type: 'fill', source: 'basemap', 'source-layer': 'buildings', minzoom: 13, paint: { 'fill-color': '#d7c9b7', 'fill-outline-color': '#c9b89f' } },
    { id: 'roads-outline', type: 'line', source: 'basemap', 'source-layer': 'roads', paint: { 'line-color': '#d4b68e', 'line-width': ['interpolate', ['linear'], ['zoom'], 12, 1.5, 17, 8] } },
    { id: 'roads', type: 'line', source: 'basemap', 'source-layer': 'roads', paint: { 'line-color': '#fffaf0', 'line-width': ['interpolate', ['linear'], ['zoom'], 12, .8, 17, 5] } },
  ] };
}
