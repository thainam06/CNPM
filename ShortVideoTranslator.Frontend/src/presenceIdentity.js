import { newDeviceId } from './deviceId.js';
let volatileId;
export function presenceDeviceId(storage) {
  try {
    storage ??= globalThis.localStorage;
    const saved = storage.getItem('smarttour-presence-device');
    if (/^[a-f\d]{8}-[a-f\d]{4}-4[a-f\d]{3}-[89ab][a-f\d]{3}-[a-f\d]{12}$/i.test(saved || '')) return saved;
    const id = newDeviceId(); storage.setItem('smarttour-presence-device', id); return id;
  } catch { return volatileId ||= newDeviceId(); }
}
