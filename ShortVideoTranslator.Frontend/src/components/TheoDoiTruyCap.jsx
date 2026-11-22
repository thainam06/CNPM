import { useEffect, useState } from 'react';
import { guiYeuCauApi, diaChiApi } from "../services/dichVuApi.js";
import { layMaTrinhDuyet } from "../utils/dinhDanhTruyCap.js";
import { moTaThietBi } from "../utils/moTaThietBi.js";
import { taoMaThietBi } from "../utils/maThietBi.js";
import { batDauTheoDoiTruyCap } from "../services/vongDoiTruyCap.js";

export function BoTheoDoiTruyCap({ children }) {
  const [blocked, setBlocked] = useState(null);
  const [now, setNow] = useState(() => Date.now());
  const isBlocked = Boolean(blocked);
  useEffect(() => {
    const receive = event => setBlocked(event.detail);
    window.addEventListener('smarttour-device-blocked', receive);
    return () => window.removeEventListener('smarttour-device-blocked', receive);
  }, []);
  useEffect(() => {
    if (!isBlocked) return;
    const controller = new AbortController();
    let checking = false;
    const check = async () => {
      if (checking) return;
      checking = true;
      try {
        const response = await guiYeuCauApi('/presence/access', { signal: AbortSignal.any([controller.signal, AbortSignal.timeout(10000)]) });
        const value = await response.json();
        if (!controller.signal.aborted) setBlocked(value.blockedUntil ? { blockedUntil: value.blockedUntil } : null);
      } catch { /* Keep the notice until the backend confirms access is restored. */ }
      finally { checking = false; }
    };
    const clock = setInterval(() => setNow(Date.now()), 1000);
    const poll = setInterval(check, 3000); check();
    return () => { controller.abort(); clearInterval(clock); clearInterval(poll); };
  }, [isBlocked]);
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
  if (blocked) return <main role="alert" style={{ maxWidth: 560, margin: '12vh auto', padding: 28, fontFamily: 'sans-serif', lineHeight: 1.7 }}>
    <h1>Truy cập đang tạm dừng</h1><p>Quản trị viên đã tạm chặn trình duyệt này.</p>
    <p>Còn khoảng <strong>{Math.max(0, Math.ceil((Date.parse(blocked.blockedUntil) - now) / 1000))} giây</strong>. Trang tự mở lại khi backend xác nhận hết hạn hoặc được bỏ chặn.</p>
    <p>Áp dụng cho yêu cầu tới ứng dụng, không ngắt kết nối Internet của thiết bị.</p>
  </main>;
  return children;
}
