import { guiYeuCauApi } from "./dichVuApi.js";
import { HangDoiSuKien } from "../utils/hangDoiThongKe.js";
import { taoMaThietBi } from "../utils/maThietBi.js";
import { moTaThietBi } from "../utils/moTaThietBi.js";
const queueKey = 'smarttour-analytics-queue';
const forgetKey = 'smarttour-analytics-forget';
const readList = key => { try { const value = JSON.parse(localStorage.getItem(key) || '[]'); return Array.isArray(value) ? value : []; } catch { return []; } };
const writeList = (key, value) => { try { localStorage.setItem(key, JSON.stringify(value)); } catch { /* Best effort on storage-restricted devices. */ } };
const allowed = () => localStorage.getItem('smarttour-consent') === 'yes' && navigator.onLine;
const queue = new HangDoiSuKien({ read: () => readList(queueKey), write: value => writeList(queueKey, value), send: events => guiYeuCauApi('/analytics/batch', {
  method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ consent: true, deviceId: localStorage.getItem('smarttour-device'), device: moTaThietBi(navigator.userAgent), events }),
}) });
export function ghiNhanSuKien(kind, placeId, language) {
  if (localStorage.getItem('smarttour-consent') !== 'yes') return;
  if (!localStorage.getItem('smarttour-device')) localStorage.setItem('smarttour-device', taoMaThietBi());
  queue.add({ id: taoMaThietBi(), kind, placeId: placeId ?? null, language, occurredAt: new Date().toISOString() });
}
export async function thuLaiXoaThongKe() {
  for (const maThietBi of readList(forgetKey)) {
    await guiYeuCauApi('/analytics/forget', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ deviceId: maThietBi }) });
    writeList(forgetKey, readList(forgetKey).filter(id => id !== maThietBi));
  }
}
export async function datDongYThongKe(value) {
  localStorage.setItem('smarttour-consent', value ? 'yes' : 'no');
  if (!value) {
    const maThietBi = localStorage.getItem('smarttour-device');
    queue.clear(); localStorage.removeItem('smarttour-device');
    if (maThietBi) writeList(forgetKey, [...new Set([...readList(forgetKey), maThietBi])]);
    await thuLaiXoaThongKe();
  }
}
export function batDauThongKe(language) {
  // Default for new visitors; preserve an explicit previous opt-out.
  if (localStorage.getItem('smarttour-consent') === null) localStorage.setItem('smarttour-consent', 'yes');
  let stopped = false; let lastHeartbeat = 0;
  const tick = async () => {
    if (stopped || document.visibilityState === 'hidden' || !navigator.onLine) return;
    try { await thuLaiXoaThongKe(); } catch { return; }
    if (!allowed() || stopped) return;
    if (Date.now() - lastHeartbeat >= 60000) { ghiNhanSuKien('heartbeat', null, language); lastHeartbeat = Date.now(); }
    await queue.flush(() => !stopped && allowed());
  };
  const timer = setInterval(tick, 30000);
  window.addEventListener('online', tick); document.addEventListener('visibilitychange', tick); tick();
  return () => { stopped = true; clearInterval(timer); window.removeEventListener('online', tick); document.removeEventListener('visibilitychange', tick); };
}
