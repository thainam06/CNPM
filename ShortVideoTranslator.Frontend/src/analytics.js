import { apiRequest } from './api';
import { EventQueue } from './analyticsQueue';
import { newDeviceId } from './deviceId';
import { deviceDescriptor } from './deviceDescriptor';
const queueKey = 'smarttour-analytics-queue';
const forgetKey = 'smarttour-analytics-forget';
const readList = key => { try { const value = JSON.parse(localStorage.getItem(key) || '[]'); return Array.isArray(value) ? value : []; } catch { return []; } };
const writeList = (key, value) => { try { localStorage.setItem(key, JSON.stringify(value)); } catch { /* Best effort on storage-restricted devices. */ } };
const allowed = () => localStorage.getItem('smarttour-consent') === 'yes' && navigator.onLine;
const queue = new EventQueue({ read: () => readList(queueKey), write: value => writeList(queueKey, value), send: events => apiRequest('/analytics/batch', {
  method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ consent: true, deviceId: localStorage.getItem('smarttour-device'), device: deviceDescriptor(navigator.userAgent), events }),
}) });
export function collectEvent(kind, placeId, language) {
  if (localStorage.getItem('smarttour-consent') !== 'yes') return;
  if (!localStorage.getItem('smarttour-device')) localStorage.setItem('smarttour-device', newDeviceId());
  queue.add({ id: newDeviceId(), kind, placeId: placeId ?? null, language, occurredAt: new Date().toISOString() });
}
export async function retryAnalyticsForget() {
  for (const deviceId of readList(forgetKey)) {
    await apiRequest('/analytics/forget', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ deviceId }) });
    writeList(forgetKey, readList(forgetKey).filter(id => id !== deviceId));
  }
}
export async function setAnalyticsConsent(value) {
  localStorage.setItem('smarttour-consent', value ? 'yes' : 'no');
  if (!value) {
    const deviceId = localStorage.getItem('smarttour-device');
    queue.clear(); localStorage.removeItem('smarttour-device');
    if (deviceId) writeList(forgetKey, [...new Set([...readList(forgetKey), deviceId])]);
    await retryAnalyticsForget();
  }
}
export function startAnalytics(language) {
  let stopped = false; let lastHeartbeat = 0;
  const tick = async () => {
    if (stopped || document.visibilityState === 'hidden' || !navigator.onLine) return;
    try { await retryAnalyticsForget(); } catch { return; }
    if (!allowed() || stopped) return;
    if (Date.now() - lastHeartbeat >= 60000) { collectEvent('heartbeat', null, language); lastHeartbeat = Date.now(); }
    await queue.flush(() => !stopped && allowed());
  };
  const timer = setInterval(tick, 30000);
  window.addEventListener('online', tick); document.addEventListener('visibilitychange', tick); tick();
  return () => { stopped = true; clearInterval(timer); window.removeEventListener('online', tick); document.removeEventListener('visibilitychange', tick); };
}
