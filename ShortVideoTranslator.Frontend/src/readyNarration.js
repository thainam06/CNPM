import { narrationKey } from './narrationKey.js';

// Use the current source/revision only; a prepared public MP3 needs no new TTS job.
export async function findReadyNarration(text, language, revision, request, signal) {
  const key = await narrationKey(text, language, revision);
  if (signal?.aborted) throw new DOMException('Đã hủy yêu cầu.', 'AbortError');
  let manifest;
  try { manifest = await (await request(`/narration/pack-manifest?language=${encodeURIComponent(language)}`, { signal })).json(); }
  catch (error) { if (error.name === 'AbortError') throw error; return null; }
  if (signal?.aborted) throw new DOMException('Đã hủy yêu cầu.', 'AbortError');
  return (Array.isArray(manifest?.files) ? manifest.files : []).find(asset => asset.key === key && asset.language === language &&
    asset.audioUrl === `/api/narration/audio/${key}` && typeof asset.text === 'string' && asset.text.trim() &&
    Number.isSafeInteger(asset.bytes) && asset.bytes > 0 && /^[a-f0-9]{64}$/.test(asset.sha256)) ?? null;
}

export async function verifyPreparedAudio(blob, asset, signal) {
  const digest = await crypto.subtle.digest('SHA-256', await blob.arrayBuffer());
  if (signal?.aborted) throw new DOMException('Đã hủy yêu cầu.', 'AbortError');
  const hash = Array.from(new Uint8Array(digest), byte => byte.toString(16).padStart(2, '0')).join('');
  if (blob.size !== asset.bytes || hash !== asset.sha256) throw new Error('MP3 đã tải không khớp nội dung kiểm tra. Hãy thử tải lại.');
  return blob;
}
