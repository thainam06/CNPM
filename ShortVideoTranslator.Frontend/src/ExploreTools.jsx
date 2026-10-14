import { useEffect, useRef, useState } from 'react';
import { apiRequest } from './api';
import { clearOffline, downloadAudioPack } from './offline';
import { GeofenceEngine } from './geofence';
import { nearbyHotset, cacheLocalization } from './localization';
import { canShareRuntime } from './runtimePolicy';
import { uiBundleReady } from './i18n';

export default function ExploreTools({ places, language, onEnter, onLanguageReadiness }) {
  const [gps, setGps] = useState(false);
  const [position, setPosition] = useState(null);
  const [message, setMessage] = useState('');
  const [working, setWorking] = useState(false);
  const [hotsetRetry, setHotsetRetry] = useState(0);
  const [shellReady, setShellReady] = useState(false);
  const [consent, setConsent] = useState(() => localStorage.getItem('smarttour-consent') === 'yes');
  const [runtimeConsent, setRuntimeConsent] = useState(false);
  const [runtimeSession, setRuntimeSession] = useState(() => localStorage.getItem('smarttour-runtime-session'));
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
      setWorking(true); readinessCallback.current?.({ language, ready: false });
      const near = currentPosition.current ? nearbyHotset(currentPosition.current, places) : [];
      const required = (near.length ? near : places.slice(0, 10)).slice(0, 3);
      setMessage(`Đang chuẩn bị giao diện và ${required.length} thuyết minh ưu tiên cho ${language}…`);
      try {
        if (!uiBundleReady(language)) throw new Error('Bundle giao diện của ngôn ngữ này chưa sẵn sàng.');
        await Promise.all(required.map(place => cacheLocalization(place, language, controller.signal)));
        if (controller.signal.aborted) return;
        setMessage(`Ngôn ngữ ${language} sẵn sàng: giao diện và ${required.length}/${required.length} thuyết minh ưu tiên đã lưu.`);
        readinessCallback.current?.({ language, ready: true });
      } catch (error) {
        if (controller.signal.aborted) return;
        setMessage(`Ngôn ngữ ${language} chưa sẵn sàng: ${error.message}`);
        readinessCallback.current?.({ language, ready: false, error: error.message });
      } finally { if (!controller.signal.aborted) setWorking(false); }
    };
    prepare(); return () => controller.abort();
  }, [language, places, hotsetRetry]);
  useEffect(() => () => sharingAbort.current?.abort(), []);
  useEffect(() => {
    if (!canShareRuntime({ consent: runtimeConsent, gps, position, sessionId: runtimeSession }) || !navigator.onLine || Date.now() - lastShare.current < 30000) return;
    const controller = new AbortController(); sharingAbort.current = controller; lastShare.current = Date.now();
    apiRequest('/runtime/location', { method: 'POST', signal: controller.signal, headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ consent: true, sessionId: runtimeSession, latitude: Math.round(position.latitude * 1000) / 1000, longitude: Math.round(position.longitude * 1000) / 1000, accuracy: position.accuracy })
    }).catch(error => { if (!controller.signal.aborted) setMessage(`Chưa chia sẻ được vị trí: ${error.message}`); });
    return () => controller.abort();
  }, [runtimeConsent, gps, position, runtimeSession]);
  useEffect(() => {
    if (!import.meta.env.PROD || !('serviceWorker' in navigator)) return;
    let cancelled = false;
    const inspect = async () => {
      const registration = await navigator.serviceWorker.ready;
      const channel = new MessageChannel();
      channel.port1.onmessage = event => { if (!cancelled) setShellReady(event.data.buildId === import.meta.env.SMARTTOUR_BUILD_ID); channel.port1.close(); };
      registration.active?.postMessage({ type: 'GET_BUILD' }, [channel.port2]);
    };
    inspect().catch(() => {});
    navigator.serviceWorker.addEventListener('controllerchange', inspect);
    return () => { cancelled = true; navigator.serviceWorker.removeEventListener('controllerchange', inspect); };
  }, []);
  useEffect(() => {
    if (!gps) return;
    const engine = new GeofenceEngine();
    let last = null;
    let lastAt = 0;
    let prefetchAt = 0;
    let blockedUntil = 0;
    let failures = 0;
    let prefetching = false;
    const controller = new AbortController();
    const reconcile = () => {
      if (last && Date.now() - lastAt < 30000) { const entered = engine.update(last, catalog.current, Date.now()); if (entered) callback.current(entered); }
      if (last && last.accuracy <= 100 && Date.now() - lastAt < 30000 && navigator.onLine && !prefetching && Date.now() - prefetchAt >= 30000 && Date.now() >= blockedUntil) {
        const nearby = nearbyHotset(last, catalog.current, 500, 3);
        prefetchAt = Date.now(); prefetching = true;
        Promise.all(nearby.map(place => cacheLocalization(place, language, controller.signal))).then(() => { failures = 0; }).catch(error => {
          if (controller.signal.aborted) return;
          failures++; blockedUntil = Date.now() + Math.min(300000, (error.status === 429 ? 60000 : 30000) * 2 ** Math.min(failures - 1, 3));
        }).finally(() => { prefetching = false; });
      }
    };
    const watch = navigator.geolocation.watchPosition(result => {
      if (Date.now() - lastAt < 5000) return;
      last = { latitude: result.coords.latitude, longitude: result.coords.longitude, accuracy: result.coords.accuracy, observedAt: Date.now() };
      lastAt = Date.now(); setPosition(last); reconcile();
    }, error => { setMessage(error.code === 1 ? 'Chưa được cấp quyền vị trí. Bạn vẫn có thể chọn địa điểm thủ công.' : 'Không lấy được vị trí. Vui lòng thử lại.'); setGps(false); }, { enableHighAccuracy: true, maximumAge: 30000, timeout: 15000 });
    const interval = setInterval(reconcile, 5000);
    return () => { controller.abort(); navigator.geolocation.clearWatch(watch); clearInterval(interval); };
  }, [gps, language]);

  async function savePack() {
    setWorking(true); setMessage('Đang kiểm tra audio tạo sẵn…');
    try { const result = await downloadAudioPack(language, places, apiRequest, (completed, total) => setMessage(`Đang lưu MP3: ${completed}/${total}`)); setMessage(`Đã lưu ${result.count} MP3 và dữ liệu địa điểm cho ${language}. ${result.missing ? `Còn ${result.missing} địa điểm chưa có audio tạo sẵn.` : ''}`); }
    catch (error) { setMessage(error.name === 'QuotaExceededError' ? 'Thiết bị không đủ dung lượng. Xóa dữ liệu offline rồi thử lại.' : error.message); }
    finally { setWorking(false); }
  }
  function prepareLanguage() { setHotsetRetry(value => value + 1); }
  async function toggleConsent(value) {
    setConsent(value); localStorage.setItem('smarttour-consent', value ? 'yes' : 'no');
    if (!value) {
      const deviceId = localStorage.getItem('smarttour-device');
      if (deviceId) try { await apiRequest('/analytics/forget', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ deviceId }) }); setMessage('Đã tắt thống kê và xóa các sự kiện đã lưu cho thiết bị này.'); }
      catch { setMessage('Đã tắt thống kê trên thiết bị. Chưa xóa được sự kiện ở server vì mất kết nối.'); }
    }
  }
  async function toggleRuntime(value) {
    sharingAbort.current?.abort();
    setRuntimeConsent(value);
    if (value) {
      const session = crypto.randomUUID(); setRuntimeSession(session); localStorage.setItem('smarttour-runtime-session', session); lastShare.current = 0;
      setMessage('Đã bật chia sẻ vị trí làm tròn với Admin. Chỉ gửi khi GPS đang bật và có kết quả mới, chính xác.');
    } else if (runtimeSession) {
      try {
        await apiRequest('/runtime/forget', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ sessionId: runtimeSession }) });
        localStorage.removeItem('smarttour-runtime-session'); setRuntimeSession(null); setMessage('Đã dừng chia sẻ, thu hồi phiên và xóa vị trí ở server.');
      } catch { setMessage('Đã dừng gửi vị trí. Chưa xóa được dữ liệu server; thử nút xóa khi có mạng. Dữ liệu tự hết hạn sau 24 giờ.'); }
    }
  }
  return <section className="explore-tools" aria-label="Tiện ích khám phá"><div><h3>Khám phá theo cách của bạn</h3><p>GPS mặc định chỉ dùng trên thiết bị. Chia sẻ vị trí với Admin cần đồng ý riêng bên dưới. Offline phát MP3 đã tải; tạo audio mới cần Internet.</p></div><div className="tool-actions">
    <button type="button" onClick={() => { if (!navigator.geolocation) { setMessage('Trình duyệt không hỗ trợ vị trí.'); return; } setMessage(''); setGps(!gps); }}>{gps ? 'Tắt định vị' : 'Bật định vị & thuyết minh gần bạn'}</button>
    <button type="button" disabled={working} onClick={prepareLanguage}>Chuẩn bị ngôn ngữ · {language}</button>
    <button type="button" disabled={working} onClick={savePack}>Tải audio offline · {language}</button>
    <button type="button" disabled={working} onClick={async () => { try { await clearOffline(); setMessage('Đã xóa dữ liệu và audio offline trên thiết bị.'); } catch { setMessage('Không xóa được dữ liệu offline.'); } }}>Xóa dữ liệu offline</button>
  </div><label className="consent"><input type="checkbox" checked={consent} onChange={event => toggleConsent(event.target.checked)} />Cho phép thống kê ẩn danh lượt xem/nghe (không gửi vị trí)</label>
    <label className="consent"><input type="checkbox" checked={runtimeConsent} onChange={event => toggleRuntime(event.target.checked)} />Đồng ý gửi vị trí làm tròn khoảng 100 m tới Admin SmartTour để giám sát demo; lưu tối đa 24 giờ, không gắn tài khoản. Mặc định tắt mỗi lần mở trang.</label>
    {runtimeSession && <button type="button" onClick={() => toggleRuntime(false)}>Xóa vị trí đã chia sẻ</button>}
    {gps && <p role="status">{position ? `Độ chính xác GPS: khoảng ${Math.round(position.accuracy)} m.` : 'Đang lấy vị trí…'} {!places.some(place => place.locationVerified) && 'Chưa có địa điểm có tọa độ xác minh để kích hoạt tự động.'}</p>}
    {shellReady && <p className="pwa-ready">✓ Giao diện đã được lưu để mở lại khi mất mạng. Audio cần được tải trước.</p>}
    {message && <p className="notice" role="status">{message}</p>}
  </section>;
}

