import { useEffect } from 'react';
import { apiRequest, apiUrl } from './api';
import { presenceDeviceId } from './presenceIdentity';
import { deviceDescriptor } from './deviceDescriptor';
import { newDeviceId } from './deviceId';
import { startPresenceTracking } from './presenceTracker';

export function PresenceProvider({ children }) {
  useEffect(() => {
    const controller = new AbortController(); const deviceId = presenceDeviceId();
    const subscribe = (target, name) => handler => { target.addEventListener(name, handler); return () => target.removeEventListener(name, handler); };
    const stop = startPresenceTracking({
      createSession: newDeviceId, isVisible: () => document.visibilityState !== 'hidden',
      subscribeVisibility: subscribe(document, 'visibilitychange'), subscribePageHide: subscribe(window, 'pagehide'),
      subscribePageShow: subscribe(window, 'pageshow'), subscribeOnline: subscribe(window, 'online'),
      sendHeartbeat: sessionId => navigator.onLine ? apiRequest('/presence/heartbeat', { method: 'POST', signal: AbortSignal.any([controller.signal, AbortSignal.timeout(10000)]),
        headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ deviceId, sessionId, device: deviceDescriptor(navigator.userAgent) }) }) : Promise.resolve(),
      sendLeave: sessionId => {
        const body = JSON.stringify({ deviceId, sessionId });
        const accepted = navigator.sendBeacon?.(apiUrl('/presence/leave'), new Blob([body], { type: 'application/json' }));
        if (!accepted) fetch(apiUrl('/presence/leave'), { method: 'POST', keepalive: true, credentials: 'include', headers: { 'Content-Type': 'application/json' }, body }).catch(() => {});
      },
    });
    return () => { controller.abort(); stop(); };
  }, []);
  return children;
}
