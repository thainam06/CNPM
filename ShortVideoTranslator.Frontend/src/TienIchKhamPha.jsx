import { useEffect, useRef, useState } from 'react';
import { guiYeuCauApi } from "./dichVuApi";
import { BoKichHoatTheoVung } from "./vungKichHoat";
import { chonDiaDiemUuTien, luuNgonNguDiaDiem } from "./chuanBiNgonNgu";
import { coTheChiaSeViTri } from "./chinhSachChiaSeViTri";
import { goiGiaoDienSanSang } from "./ngonNguGiaoDien";

export default function TienIchKhamPha({ places, language, onEnter, location, onLanguageReadiness, onMessage, preparationRetry = 0 }) {
  const { enabled: gps, position } = location;
  const [runtimeSession, setRuntimeSession] = useState(() => crypto.randomUUID());
  const sharedLocation = useRef(false);
  const messageCallback = useRef(onMessage);
  useEffect(() => { messageCallback.current = onMessage; }, [onMessage]);
  const sharingAbort = useRef(null);
  const lastShare = useRef(0);
  const callback = useRef(onEnter);
  const catalog = useRef(places);
  const readinessCallback = useRef(onLanguageReadiness);
  const currentPosition = useRef(position);
  useEffect(() => { callback.current = onEnter; catalog.current = places; }, [onEnter, places]);
  useEffect(() => { readinessCallback.current = onLanguageReadiness; currentPosition.current = position; }, [onLanguageReadiness, position]);
  useEffect(() => {
    if (!places.length) return;
    const controller = new AbortController();
    const prepare = async () => {
      await Promise.resolve();
      if (controller.signal.aborted) return;
      readinessCallback.current?.({ language, ready: false });
      const near = currentPosition.current ? chonDiaDiemUuTien(currentPosition.current, places) : [];
      const required = (near.length ? near : places.slice(0, 10)).slice(0, 3);
      try {
        if (!goiGiaoDienSanSang(language)) throw new Error('Bundle giao diện của ngôn ngữ này chưa sẵn sàng.');
        await Promise.all(required.map(place => luuNgonNguDiaDiem(place, language, controller.signal)));
        if (controller.signal.aborted) return;
        readinessCallback.current?.({ language, ready: true });
      } catch (error) {
        if (controller.signal.aborted) return;
        readinessCallback.current?.({ language, ready: false, error: error.message });
      }
    };
    prepare(); return () => controller.abort();
  }, [language, places, preparationRetry]);
  useEffect(() => () => sharingAbort.current?.abort(), []);
  useEffect(() => {
    if (!coTheChiaSeViTri({ consent: true, gps, position, sessionId: runtimeSession }) || !navigator.onLine || Date.now() - lastShare.current < 30000) return;
    const controller = new AbortController(); sharingAbort.current = controller; lastShare.current = Date.now(); sharedLocation.current = true;
    guiYeuCauApi('/runtime/location', { method: 'POST', signal: controller.signal, headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ consent: true, sessionId: runtimeSession, latitude: Math.round(position.latitude * 1000) / 1000, longitude: Math.round(position.longitude * 1000) / 1000, accuracy: position.accuracy })
    }).catch(error => { if (!controller.signal.aborted) messageCallback.current?.(`Chưa chia sẻ được vị trí: ${error.message}`); });
    return () => controller.abort();
  }, [gps, position, runtimeSession]);
  useEffect(() => {
    if (gps || !sharedLocation.current) return;
    sharedLocation.current = false;
    const previous = runtimeSession;
    queueMicrotask(() => { setRuntimeSession(crypto.randomUUID()); lastShare.current = 0; });
    guiYeuCauApi('/runtime/forget', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ sessionId: previous }) })
      .catch(() => messageCallback.current?.('Đã dừng gửi vị trí. Chưa xóa được dữ liệu đã chia sẻ; dữ liệu tự hết hạn sau 24 giờ.'));
  }, [gps, runtimeSession]);
  useEffect(() => {
    if (!gps) return;
    const engine = new BoKichHoatTheoVung();
    let prefetchAt = 0;
    let blockedUntil = 0;
    let failures = 0;
    let prefetching = false;
    const controller = new AbortController();
    const reconcile = () => {
      const last = currentPosition.current;
      const lastAt = last?.observedAt ?? 0;
      if (!last || Date.now() - lastAt >= 30000) engine.pending.clear();
      if (last && Date.now() - lastAt < 30000) { const entered = engine.update(last, catalog.current, Date.now()); if (entered) callback.current(entered); }
      if (last && last.accuracy <= 100 && Date.now() - lastAt < 30000 && navigator.onLine && !prefetching && Date.now() - prefetchAt >= 30000 && Date.now() >= blockedUntil) {
        const nearby = chonDiaDiemUuTien(last, catalog.current, 500, 3);
        prefetchAt = Date.now(); prefetching = true;
        Promise.all(nearby.map(place => luuNgonNguDiaDiem(place, language, controller.signal))).then(() => { failures = 0; }).catch(error => {
          if (controller.signal.aborted) return;
          failures++; blockedUntil = Date.now() + Math.min(300000, (error.status === 429 ? 60000 : 30000) * 2 ** Math.min(failures - 1, 3));
        }).finally(() => { prefetching = false; });
      }
    };
    reconcile();
    const interval = setInterval(reconcile, 5000);
    return () => { controller.abort(); clearInterval(interval); };
  }, [gps, language]);

  // Background preparation and geofencing continue without a separate settings panel.
  return null;
}
