import { lazy, Suspense, useEffect, useRef, useState } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { uiText } from './i18n';
import { downloadMapPack, getMapPack, removeMapPack } from './mapPack';
const OfflineMap = lazy(() => import('./OfflineMap'));

// Tourism viewport from the initial sourced coordinates, padded by 0.01 degrees; all five POIs fit.
// This rectangle is a tourism viewport, not an administrative boundary.
const districtBounds = [[10.7625222, 106.6853694], [10.7900000, 106.7100000]];

function CloudMap({ places, selectedId, onSelect, language = 'vi', onUnavailable }) {
  const container = useRef(null);
  const instance = useRef(null);
  const layers = useRef(null);
  const callback = useRef(onSelect);
  const unavailable = useRef(onUnavailable);
  const [tileError, setTileError] = useState(false);
  const located = places.filter(place => place.locationVerified && Number.isFinite(place.latitude) && Number.isFinite(place.longitude));
  useEffect(() => { callback.current = onSelect; }, [onSelect]);
  useEffect(() => { unavailable.current = onUnavailable; }, [onUnavailable]);
  useEffect(() => {
    const map = L.map(container.current, { scrollWheelZoom: false }).fitBounds(districtBounds);
    instance.current = map;
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19, attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap contributors</a>',
    }).on('tileerror', () => { setTileError(true); unavailable.current?.(); }).addTo(map);
    layers.current = L.layerGroup().addTo(map);
    const observer = new ResizeObserver(() => map.invalidateSize()); observer.observe(container.current);
    return () => { observer.disconnect(); map.remove(); instance.current = null; layers.current = null; };
  }, []);
  useEffect(() => {
    if (!instance.current || !layers.current) return;
    layers.current.clearLayers();
    const valid = places.filter(place => place.locationVerified && Number.isFinite(place.latitude) && Number.isFinite(place.longitude));
    for (const place of valid) {
      const popup = document.createElement('div');
      const title = document.createElement('strong'); title.textContent = place.name; popup.append(title);
      const address = document.createElement('p'); address.textContent = place.address || ''; popup.append(address);
      const button = document.createElement('button'); button.type = 'button'; button.textContent = 'Xem chi tiết & nghe thuyết minh';
      button.addEventListener('click', () => callback.current(place.id)); popup.append(document.createElement('br'), button);
      const marker = L.circleMarker([place.latitude, place.longitude], { radius: place.id === selectedId ? 11 : 8, color: '#fff', weight: 3, fillColor: place.id === selectedId ? '#dd7d22' : '#b95420', fillOpacity: 1 }).bindPopup(popup).addTo(layers.current);
      marker.on('click', () => callback.current(place.id));
      if (place.id === selectedId) { instance.current.setView([place.latitude, place.longitude], 16); marker.openPopup(); }
    }
  }, [places, selectedId]);
  function showAll() {
    instance.current?.fitBounds(located.length ? L.latLngBounds(located.map(place => [place.latitude, place.longitude])).pad(.25) : districtBounds, { maxZoom: 16 });
  }
  return <section className="home-map-section" id="ban-do" aria-label="Bản đồ khám phá Quận 1">
    <div className="home-map-heading"><div><span className="category">{uiText(language, 'map')}</span><h2>{uiText(language, 'mapTitle')}</h2><p>{uiText(language, 'mapHint')}</p></div><button onClick={showAll}>{uiText(language, 'mapAll')}</button></div>
    <div ref={container} className="home-map" role="region" aria-label="Bản đồ tương tác OpenStreetMap khu vực Quận 1" />
    <div className="home-map-note">{located.length ? <div className="map-place-list">{located.map(place => <button key={place.id} onClick={() => callback.current(place.id)} aria-pressed={place.id === selectedId}>📍 {place.name}</button>)}</div> : <p>Bản đồ nền đang hiển thị khu vực Quận 1. Các địa điểm demo chưa có tọa độ đã xác minh nên chưa đặt ghim.</p>}
      {tileError && <p className="notice" role="alert">Không tải được một số ô bản đồ. Kiểm tra Internet; danh sách địa điểm vẫn dùng được.</p>}
      <small>Khung xem bao quanh các điểm tham quan, không phải ranh giới hành chính. Tham khảo <a href="https://commons.wikimedia.org/wiki/Category:District_1,_Ho_Chi_Minh_City" target="_blank" rel="noreferrer">Quận 1 trên Wikimedia Commons</a>. Ghim là vị trí tham khảo của công trình từ nguồn tọa độ đã dẫn, không phải cổng vào đã đo GPS. Bản đồ nền cần Internet.</small>
    </div>
  </section>;
}

export default function HomeMap(props) {
  const [mode, setMode] = useState(() => localStorage.getItem('smarttour-map-mode:district1') || 'cloud');
  const [pack, setPack] = useState(null);
  const [ready, setReady] = useState(false);
  const [offline, setOffline] = useState(!navigator.onLine);
  const [cloudFailed, setCloudFailed] = useState(false);
  const [working, setWorking] = useState(false);
  const [message, setMessage] = useState('');
  const downloading = useRef(null);
  useEffect(() => {
    let active = true;
    getMapPack().then(value => { if (active) { setPack(value); setReady(true); } });
    const lost = () => setOffline(true); const restored = () => setOffline(false);
    window.addEventListener('offline', lost); window.addEventListener('online', restored);
    return () => { active = false; downloading.current?.abort(); window.removeEventListener('offline', lost); window.removeEventListener('online', restored); };
  }, []);
  useEffect(() => {
    if (mode !== 'hybrid' || offline || !cloudFailed) return;
    // Probe the actual cloud provider before returning; avoid online/offline flip-flop.
    let disposed = false; let image;
    const timer = setInterval(() => {
      image = new Image();
      image.onload = () => { if (!disposed) setCloudFailed(false); };
      const zoom = 15, lon = 106.698, lat = 10.777;
      const x = Math.floor((lon + 180) / 360 * 2 ** zoom);
      const y = Math.floor((1 - Math.asinh(Math.tan(lat * Math.PI / 180)) / Math.PI) / 2 * 2 ** zoom);
      image.src = `https://tile.openstreetmap.org/${zoom}/${x}/${y}.png`;
    }, 15000);
    return () => { disposed = true; clearInterval(timer); if (image) image.onload = null; };
  }, [mode, offline, cloudFailed]);
  const local = mode === 'offline' || (mode === 'hybrid' && (offline || cloudFailed));
  function selectMode(value) { setMode(value); localStorage.setItem('smarttour-map-mode:district1', value); }
  async function download() {
    const controller = new AbortController(); downloading.current = controller; setWorking(true);
    try { const value = await downloadMapPack(controller.signal, setMessage); setPack(value); setMessage('Đã xác minh và lưu gói Quận 1. Có thể chọn Offline hoặc Hybrid.'); }
    catch (error) { if (!controller.signal.aborted) setMessage(error.name === 'QuotaExceededError' ? 'Không đủ bộ nhớ. Gói cũ được giữ; dọn dữ liệu hoặc thử lại.' : error.message); }
    finally { if (!controller.signal.aborted) setWorking(false); }
  }
  return <div className="map-pack-wrapper">
    <div className="map-pack-toolbar"><label>Chế độ bản đồ<select value={mode} onChange={event => selectMode(event.target.value)}><option value="cloud">Online · OpenStreetMap</option><option value="offline">Offline · gói Quận 1</option><option value="hybrid">Hybrid · tự chuyển khi mất mạng</option></select></label>
      <button disabled={working} onClick={download}>{working ? 'Đang tải bản đồ…' : pack ? 'Kiểm tra & cập nhật gói bản đồ' : 'Tải bản đồ Quận 1 offline'}</button>
      {pack && <button disabled={working} onClick={async () => { await removeMapPack(); setPack(null); selectMode('cloud'); setMessage('Đã xóa gói bản đồ local; có thể tải lại.'); }}>Xóa gói bản đồ</button>}
    </div>
    {message && <p className="notice" role="status">{message}</p>}
    {local ? <section id="ban-do" className="home-map-section"><div className="home-map-heading"><div><span className="category">{mode === 'hybrid' ? 'HYBRID · ĐANG DÙNG GÓI ĐÃ LƯU' : 'BẢN ĐỒ OFFLINE'}</span><h2>Quận 1 trên bản đồ</h2></div></div>
      {!ready ? <p>Đang kiểm tra gói đã lưu…</p> : pack ? <Suspense fallback={<p>Đang mở bản đồ vector…</p>}><OfflineMap {...props} pack={pack} /></Suspense> : <p className="notice">Chưa có gói bản đồ trên thiết bị. Khi có Internet, nhấn “Tải bản đồ Quận 1 offline”.</p>}
    </section> : <CloudMap {...props} onUnavailable={() => setCloudFailed(true)} />}
  </div>;
}
