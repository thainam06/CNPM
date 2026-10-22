export const baseUrl = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '');
import { audioKey, offlineGet, offlineSet, offlineSetMany, offlineNotice, readOfflineAudio, saveOfflineAudio } from './offline';
import { findReadyNarration, verifyPreparedAudio } from './readyNarration';
export const apiUrl = (path) => `${baseUrl}/api${path}`;

let refreshing;
async function refreshSession() {
  if (!refreshing) refreshing = (async () => {
    const csrf = await fetch(apiUrl('/auth/csrf'), { credentials: 'include' });
    if (!csrf.ok) return false;
    const { token } = await csrf.json();
    const response = await fetch(apiUrl('/auth/refresh'), { method: 'POST', credentials: 'include', headers: { 'X-CSRF-Token': token } });
    return response.ok;
  })().catch(() => false).finally(() => { refreshing = null; });
  return refreshing;
}
export async function apiRequest(path, options = {}, allowRefresh = true) {
  let response;
  try {
    response = await fetch(apiUrl(path), { credentials: 'include', ...options });
  } catch (error) {
    if (error.name === 'AbortError') throw error;
    throw new Error('Không kết nối được Backend. Kiểm tra API đang chạy và cấu hình kết nối.');
  }
  if (response.status === 401 && allowRefresh && (path === '/auth/me' || path.startsWith('/runtime/window') || /^\/(admin|owner|ai|media)(\/|$)/.test(path)) && await refreshSession()) {
    const next = { ...options };
    if (next.method && next.method !== 'GET') {
      const csrf = await readApi('/auth/csrf'); next.headers = { ...next.headers, 'X-CSRF-Token': csrf.token };
    }
    return apiRequest(path, next, false);
  }
  if (!response.ok && response.status !== 304) {
    const problem = await response.json().catch(() => null);
    const error = new Error(problem?.message || problem?.detail || problem?.title || (response.status >= 500
      ? `Backend không phản hồi hoặc gặp lỗi (HTTP ${response.status}). Kiểm tra API và thử lại.`
      : `Yêu cầu thất bại (HTTP ${response.status}).`));
    error.status = response.status;
    throw error;
  }
  return response;
}

export const readApi = async (path, signal) => (await apiRequest(path, { signal })).json();
export async function mutateApi(path, method = 'POST', body = {}, signal) {
  const { token } = await readApi('/auth/csrf', signal);
  const form = body instanceof FormData;
  const response = await apiRequest(path, { method, signal, headers: { 'X-CSRF-Token': token, ...(form ? {} : { 'Content-Type': 'application/json' }) }, body: form ? body : JSON.stringify(body) });
  return response.status === 204 ? null : response.json();
}

const canFallback = (error) => error.name !== 'AbortError' && (!error.status || error.status >= 500);
export async function getPlaces(signal) {
  try {
    const sync = await offlineGet('catalog-sync:district1');
    const previous = await offlineGet('places:district1');
    const query = sync && previous ? `?since=${encodeURIComponent(sync.updatedAt)}` : '';
    const response = await apiRequest(`/content${query}`, { signal, headers: sync && previous ? { 'If-None-Match': sync.etag } : {} });
    if (response.status === 304 && previous) { offlineNotice(''); return previous; }
    const snapshot = await response.json();
    const merged = new Map(snapshot.syncMode === 'delta' && previous ? previous.map(p => [p.id, p]) : []);
    for (const id of snapshot.removedIds) merged.delete(id);
    for (const item of snapshot.items) merged.set(item.place.id, item.place);
    const data = [...merged.values()].sort((a, b) => a.id - b.id);
    await offlineSetMany([['places:district1', data], ['catalog-sync:district1', { updatedAt: snapshot.updatedAt, etag: response.headers.get('ETag') }]]).catch(() => {}); offlineNotice(''); return data;
  } catch (error) {
    const cached = canFallback(error) ? await offlineGet('places:district1') : null;
    if (!cached) throw error;
    offlineNotice('Đang dùng dữ liệu đã lưu. Thông tin có thể cũ; nội dung chưa tải sẽ cần Internet.'); return cached;
  }
}
export async function getPlace(id, signal) {
  try { return await (await apiRequest(`/places/${id}`, { signal })).json(); }
  catch (error) {
    const place = canFallback(error) ? (await offlineGet('places:district1'))?.find(item => item.id === id) : null;
    if (!place) throw error; return place;
  }
}
export async function translate(text, language, signal, revision = '') {
  const key = await audioKey(text, language, revision);
  try {
    const ready = await findReadyNarration(text, language, revision, apiRequest, signal);
    if (ready) {
      const data = { text: ready.text, language, provider: ready.provider || (language === 'vi' ? 'original' : 'MyMemory'), isMachineTranslation: language !== 'vi' && ready.provider !== 'reviewed' };
      await offlineSet(`translation:${key}`, data).catch(() => {}); return data;
    }
    const data = await (await apiRequest('/narration/translate', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ text, language, revision }), signal })).json();
    await offlineSet(`translation:${key}`, data).catch(() => {}); return data;
  } catch (error) {
    if (!canFallback(error)) throw error;
    const cached = await offlineGet(`translation:${key}`);
    if (cached) { offlineNotice('Đang dùng bản dịch đã tải trước đó.'); return cached; }
    if (language === 'vi') return { text, language, provider: 'original', isMachineTranslation: false };
    throw error;
  }
}
export async function createSpeech(text, language, signal, revision = '') {
  const key = await audioKey(text, language, revision);
  const cached = await readOfflineAudio(key);
  if (signal?.aborted) throw new DOMException('Đã hủy yêu cầu.', 'AbortError');
  if (cached) return cached;
  const ready = await findReadyNarration(text, language, revision, apiRequest, signal);
  const blob = ready
    ? await verifyPreparedAudio(await (await apiRequest(ready.audioUrl.replace(/^\/api/, ''), { signal, cache: 'no-store' })).blob(), ready, signal)
    : await (await apiRequest('/narration/speech', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ text, language, revision }), signal })).blob();
  if (signal?.aborted) throw new DOMException('Đã hủy yêu cầu.', 'AbortError');
  await saveOfflineAudio(key, blob, language).catch(() => {}); return blob;
}
