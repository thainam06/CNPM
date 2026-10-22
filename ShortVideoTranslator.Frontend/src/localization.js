import { apiRequest } from './api';
import { audioKey, readOfflineAudio, saveOfflineAudio, offlineSet } from './offline';
import { nearbyHotset } from './geofence';
export { nearbyHotset };

export async function cacheLocalization(place, language, signal) {
  const key = await audioKey(place.description, language, place.narrationRevisions?.[language]);
  if (await readOfflineAudio(key)) return;
  const asset = await (await apiRequest(`/localizations/${place.id}`, { method: 'POST', signal, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ language }) })).json();
  if (asset.key !== key) throw new Error('Bản dịch vừa thay đổi. Tải lại danh sách địa điểm.');
  const blob = await (await apiRequest(asset.audioUrl.replace(/^\/api/, ''), { signal, cache: 'no-store' })).blob();
  const hash = Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', await blob.arrayBuffer())), b => b.toString(16).padStart(2, '0')).join('');
  if (hash !== asset.sha256 || blob.size !== asset.bytes) throw new Error('Audio tải trước không khớp checksum.');
  if (signal?.aborted) throw new DOMException('Đã hủy', 'AbortError');
  await saveOfflineAudio(key, blob, language);
  await offlineSet(`translation:${key}`, { text: asset.text, language, provider: asset.provider || (language === 'vi' ? 'original' : 'MyMemory'), isMachineTranslation: language !== 'vi' && asset.provider !== 'reviewed' });
}
