import { useEffect } from 'react';
import { guiYeuCauApi, diaChiApi } from "../services/dichVuApi.js";
import { layMaTrinhDuyet } from "../utils/dinhDanhTruyCap.js";
import { moTaThietBi } from "../utils/moTaThietBi.js";
import { taoMaThietBi } from "../utils/maThietBi.js";
import { batDauTheoDoiTruyCap } from "../services/vongDoiTruyCap.js";

export function BoTheoDoiTruyCap({ children }) {
  useEffect(() => {
    const controller = new AbortController(); const maThietBi = layMaTrinhDuyet();
    const subscribe = (target, name) => handler => { target.addEventListener(name, handler); return () => target.removeEventListener(name, handler); };
    const stop = batDauTheoDoiTruyCap({
      createSession: taoMaThietBi, isVisible: () => document.visibilityState !== 'hidden',
      subscribeVisibility: subscribe(document, 'visibilitychange'), subscribePageHide: subscribe(window, 'pagehide'),
      subscribePageShow: subscribe(window, 'pageshow'), subscribeOnline: subscribe(window, 'online'),
      sendHeartbeat: sessionId => navigator.onLine ? guiYeuCauApi('/presence/heartbeat', { method: 'POST', signal: AbortSignal.any([controller.signal, AbortSignal.timeout(10000)]),
        headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ deviceId: maThietBi, sessionId, device: moTaThietBi(navigator.userAgent) }) }) : Promise.resolve(),
      sendLeave: sessionId => {
        const body = JSON.stringify({ deviceId: maThietBi, sessionId });
        const accepted = navigator.sendBeacon?.(diaChiApi('/presence/leave'), new Blob([body], { type: 'application/json' }));
        if (!accepted) fetch(diaChiApi('/presence/leave'), { method: 'POST', keepalive: true, credentials: 'include', headers: { 'Content-Type': 'application/json' }, body }).catch(() => {});
      },
    });
    return () => { controller.abort(); stop(); };
  }, []);
  return children;
}
