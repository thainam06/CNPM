import { lapKeHoachLuuAmThanh } from "./chinhSachBoNho";
export { taoKhoaThuyetMinh as audioKey } from "./khoaThuyetMinh.js";
import { taoKhoaThuyetMinh as audioKey } from "./khoaThuyetMinh.js";
let database;
let audioWrites = Promise.resolve();
function db() {
  if (!database) database = new Promise((resolve, reject) => {
    const request = indexedDB.open('SmartTourOffline', 1);
    request.onupgradeneeded = () => request.result.createObjectStore('assets');
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
  return database;
}
export async function docNgoaiTuyen(key) {
  try { const database = await db(); return await new Promise((resolve, reject) => { const request = database.transaction('assets').objectStore('assets').get(key); request.onsuccess = () => resolve(request.result); request.onerror = () => reject(request.error); }); }
  catch { return undefined; }
}
export async function ghiNgoaiTuyen(key, value) {
  return ghiNhieuNgoaiTuyen([[key, value]]);
}
export async function ghiNhieuNgoaiTuyen(entries) {
  const database = await db();
  return new Promise((resolve, reject) => {
    const transaction = database.transaction('assets', 'readwrite');
    for (const [key, value] of entries) transaction.objectStore('assets').put(value, key);
    transaction.oncomplete = resolve; transaction.onerror = () => reject(transaction.error); transaction.onabort = () => reject(transaction.error);
  });
}
export async function xoaNgoaiTuyen() {
  const database = await db();
  await new Promise((resolve, reject) => { const transaction = database.transaction('assets', 'readwrite'); transaction.objectStore('assets').clear(); transaction.oncomplete = resolve; transaction.onerror = () => reject(transaction.error); });
}
export function luuAmThanhNgoaiTuyen(key, blob, language) {
  const job = audioWrites.catch(() => {}).then(async () => {
    const index = await docNgoaiTuyen('audio-index') || {};
    const plan = lapKeHoachLuuAmThanh(index, { key, language }, Date.now(), localStorage.getItem('smarttour-language'));
    const database = await db();
    const write = (removed) => new Promise((resolve, reject) => {
      const transaction = database.transaction('assets', 'readwrite');
      const assets = transaction.objectStore('assets');
      for (const old of removed) { assets.delete(`audio:${old}`); assets.delete(`translation:${old}`); delete plan.entries[old]; }
      assets.put(blob, `audio:${key}`); assets.put(plan.entries, 'audio-index');
      transaction.oncomplete = resolve;
      transaction.onabort = () => reject(transaction.error);
      transaction.onerror = () => reject(transaction.error);
    });
    try { await write(plan.removed); }
    catch (error) {
      if (error?.name !== 'QuotaExceededError') throw error;
      const oldest = Object.entries(plan.entries).filter(([id]) => id !== key).sort((a, b) => a[1].accessedAt - b[1].accessedAt).slice(0, Math.max(1, Math.ceil(Object.keys(plan.entries).length / 2))).map(([id]) => id);
      await write([...plan.removed, ...oldest]);
      thongBaoNgoaiTuyen('Bộ nhớ gần đầy. Đã dọn audio ít dùng để lưu nội dung mới.');
    }
  });
  audioWrites = job; return job;
}
export async function docAmThanhNgoaiTuyen(key) {
  const blob = await docNgoaiTuyen(`audio:${key}`);
  if (blob) {
    const job = audioWrites.catch(() => {}).then(async () => {
      const index = await docNgoaiTuyen('audio-index');
      if (index?.[key]) { index[key].accessedAt = Date.now(); await ghiNgoaiTuyen('audio-index', index); }
    });
    audioWrites = job; await job.catch(() => {});
  }
  return blob;
}
export function thongBaoNgoaiTuyen(message) { window.dispatchEvent(new CustomEvent('smarttour-offline', { detail: message })); }
export function baoTaiNguyenThayDoi(catalog = false) { window.dispatchEvent(new CustomEvent('smarttour-assets-changed', { detail: { catalog } })); }
export function kichHoatGoiNgoaiTuyen(staged, signal) {
  const job = audioWrites.catch(() => {}).then(async () => {
    if (signal?.aborted) throw new DOMException('Đã hủy tải gói.', 'AbortError');
    let index = await docNgoaiTuyen('audio-index') || {}; const removed = new Set();
    for (const asset of staged.audio) {
      const plan = lapKeHoachLuuAmThanh(index, asset, Date.now(), localStorage.getItem('smarttour-language'));
      index = plan.entries; plan.removed.forEach(key => removed.add(key));
    }
    const database = await db();
    if (signal?.aborted) throw new DOMException('Đã hủy tải gói.', 'AbortError');
    await new Promise((resolve, reject) => {
      const transaction = database.transaction('assets', 'readwrite'); const assets = transaction.objectStore('assets');
      const cancel = () => { try { transaction.abort(); } catch { /* Transaction already complete. */ } };
      signal?.addEventListener('abort', cancel, { once: true });
      for (const key of removed) { assets.delete(`audio:${key}`); assets.delete(`translation:${key}`); }
      for (const [key, value] of staged.entries) assets.put(value, key);
      assets.put(index, 'audio-index');
      transaction.oncomplete = () => { signal?.removeEventListener('abort', cancel); resolve(); };
      transaction.onabort = transaction.onerror = () => { signal?.removeEventListener('abort', cancel); reject(signal?.aborted ? new DOMException('Đã hủy tải gói.', 'AbortError') : transaction.error); };
    });
    baoTaiNguyenThayDoi(true);
  });
  audioWrites = job; return job;
}

export async function taiGoiAmThanh(language, places, request, progress) {
  const manifest = await (await request(`/narration/pack-manifest?language=${language}`)).json();
  const wanted = new Map(await Promise.all(places.map(async place => [await audioKey(place.description, language, place.narrationRevisions?.[language]), place])));
  const files = manifest.files.filter(file => wanted.has(file.key));
  if (!files.length) throw new Error('Chưa có MP3 tạo sẵn cho ngôn ngữ này. Admin cần tạo audio trước khi tải gói.');
  await ghiNgoaiTuyen('places:district1', places);
  for (let index = 0; index < files.length; index++) {
    const asset = files[index];
    const response = await request(asset.audioUrl.replace(/^\/api/, ''), { cache: 'no-store' });
    const blob = await response.blob();
    const hash = Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', await blob.arrayBuffer())), byte => byte.toString(16).padStart(2, '0')).join('');
    if (hash !== asset.sha256 || blob.size !== asset.bytes) throw new Error('MP3 tải xuống không khớp checksum. Gói chưa được hoàn tất.');
    await luuAmThanhNgoaiTuyen(asset.key, blob, language);
    await ghiNgoaiTuyen(`translation:${asset.key}`, { text: asset.text, language, provider: asset.provider || (language === 'vi' ? 'original' : 'MyMemory'), isMachineTranslation: language !== 'vi' && asset.provider !== 'reviewed' });
    progress(index + 1, files.length);
  }
  const missing = places.length - files.length;
  return { count: files.length, missing };
}
