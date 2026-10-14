import { apiRequest } from './api';
export function collectEvent(kind, placeId, language) {
  if (localStorage.getItem('smarttour-consent') !== 'yes') return;
  let deviceId = localStorage.getItem('smarttour-device');
  if (!deviceId) { deviceId = crypto.randomUUID(); localStorage.setItem('smarttour-device', deviceId); }
  apiRequest('/analytics/events', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ consent: true, deviceId, kind, placeId, language }) }).catch(() => {});
}
