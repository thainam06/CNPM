export const diaChiGoc = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '');
import { audioKey, docNgoaiTuyen, ghiNgoaiTuyen, ghiNhieuNgoaiTuyen, thongBaoNgoaiTuyen, docAmThanhNgoaiTuyen, luuAmThanhNgoaiTuyen } from "./khoNgoaiTuyen";
import { timThuyetMinhSanSang, xacMinhAmThanh } from "./thuyetMinhSanSang";
export const diaChiApi = (path) => `${diaChiGoc}/api${path}`;

let refreshing;
async function refreshSession() {
  if (!refreshing) refreshing = (async () => {
    const csrf = await fetch(diaChiApi('/auth/csrf'), { credentials: 'include' });
    if (!csrf.ok) return false;
    const { token } = await csrf.json();
    const response = await fetch(diaChiApi('/auth/refresh'), { method: 'POST', credentials: 'include', headers: { 'X-CSRF-Token': token } });
    return response.ok;
  })().catch(() => false).finally(() => { refreshing = null; });
  return refreshing;
}
export async function guiYeuCauApi(path, options = {}, allowRefresh = true) {
  let response;
  try {
    response = await fetch(diaChiApi(path), { credentials: 'include', ...options });
  } catch (error) {
    if (error.name === 'AbortError') throw error;
    throw new Error('Không kết nối được Backend. Kiểm tra API đang chạy và cấu hình kết nối.');
  }
  if (response.status === 401 && allowRefresh && (path === '/auth/me' || path.startsWith('/runtime/window') || /^\/(admin|owner|ai|media)(\/|$)/.test(path)) && await refreshSession()) {
    const next = { ...options };
    if (next.method && next.method !== 'GET') {
      const csrf = await docApi('/auth/csrf'); next.headers = { ...next.headers, 'X-CSRF-Token': csrf.token };
    }
    return guiYeuCauApi(path, next, false);
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

export const docApi = async (path, signal) => (await guiYeuCauApi(path, { signal })).json();
export async function ghiApi(path, method = 'POST', body = {}, signal) {
  const { token } = await docApi('/auth/csrf', signal);
  const form = body instanceof FormData;
  const response = await guiYeuCauApi(path, { method, signal, headers: { 'X-CSRF-Token': token, ...(form ? {} : { 'Content-Type': 'application/json' }) }, body: form ? body : JSON.stringify(body) });
  return response.status === 204 ? null : response.json();
}

const canFallback = (error) => error.name !== 'AbortError' && (!error.status || error.status >= 500);
export async function layDanhSachDiaDiem(signal) {
  try {
    const sync = await docNgoaiTuyen('catalog-sync:district1');
    const previous = await docNgoaiTuyen('places:district1');
    const query = sync && previous ? `?since=${encodeURIComponent(sync.updatedAt)}` : '';
    const response = await guiYeuCauApi(`/content${query}`, { signal, headers: sync && previous ? { 'If-None-Match': sync.etag } : {} });
    if (response.status === 304 && previous) { thongBaoNgoaiTuyen(''); return previous; }
    const snapshot = await response.json();
    const merged = new Map(snapshot.syncMode === 'delta' && previous ? previous.map(p => [p.id, p]) : []);
    for (const id of snapshot.removedIds) merged.delete(id);
    for (const item of snapshot.items) merged.set(item.place.id, item.place);
    const data = [...merged.values()].sort((a, b) => a.id - b.id);
    await ghiNhieuNgoaiTuyen([['places:district1', data], ['catalog-sync:district1', { updatedAt: snapshot.updatedAt, etag: response.headers.get('ETag') }]]).catch(() => {}); thongBaoNgoaiTuyen(''); return data;
  } catch (error) {
    const cached = canFallback(error) ? await docNgoaiTuyen('places:district1') : null;
    if (!cached) throw error;
    thongBaoNgoaiTuyen('Đang dùng dữ liệu đã lưu. Thông tin có thể cũ; nội dung chưa tải sẽ cần Internet.'); return cached;
  }
}
export async function layChiTietDiaDiem(id, signal) {
  try { return await (await guiYeuCauApi(`/places/${id}`, { signal })).json(); }
  catch (error) {
    const place = canFallback(error) ? (await docNgoaiTuyen('places:district1'))?.find(item => item.id === id) : null;
    if (!place) throw error; return place;
  }
}
export async function dichNoiDung(text, language, signal, revision = '') {
  const key = await audioKey(text, language, revision);
  try {
    const ready = await timThuyetMinhSanSang(text, language, revision, guiYeuCauApi, signal);
    if (ready) {
      const data = { text: ready.text, language, provider: ready.provider || (language === 'vi' ? 'original' : 'MyMemory'), isMachineTranslation: language !== 'vi' && ready.provider !== 'reviewed' };
      await ghiNgoaiTuyen(`translation:${key}`, data).catch(() => {}); return data;
    }
    const data = await (await guiYeuCauApi('/narration/translate', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ text, language, revision }), signal })).json();
    await ghiNgoaiTuyen(`translation:${key}`, data).catch(() => {}); return data;
  } catch (error) {
    if (!canFallback(error)) throw error;
    const cached = await docNgoaiTuyen(`translation:${key}`);
    if (cached) { thongBaoNgoaiTuyen('Đang dùng bản dịch đã tải trước đó.'); return cached; }
    if (language === 'vi') return { text, language, provider: 'original', isMachineTranslation: false };
    throw error;
  }
}
export async function taoThuyetMinh(text, language, signal, revision = '') {
  const key = await audioKey(text, language, revision);
  const cached = await docAmThanhNgoaiTuyen(key);
  if (signal?.aborted) throw new DOMException('Đã hủy yêu cầu.', 'AbortError');
  if (cached) return cached;
  const ready = await timThuyetMinhSanSang(text, language, revision, guiYeuCauApi, signal);
  const blob = ready
    ? await xacMinhAmThanh(await (await guiYeuCauApi(ready.audioUrl.replace(/^\/api/, ''), { signal, cache: 'no-store' })).blob(), ready, signal)
    : await (await guiYeuCauApi('/narration/speech', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ text, language, revision }), signal })).blob();
  if (signal?.aborted) throw new DOMException('Đã hủy yêu cầu.', 'AbortError');
  await luuAmThanhNgoaiTuyen(key, blob, language).catch(() => {}); return blob;
}
