import { useState } from 'react';
import PlaceImage from './PlaceImage';
import { rankedPlaces } from './nearbyPlaces.js';
import { useNearbyLocation } from './useNearbyLocation';
import './NearbyPlaces.css';

export default function NearbyPlaces({ places, onSelect }) {
  const { enabled, position, error, start, clear } = useNearbyLocation();
  const [radius, setRadius] = useState(3000);
  const results = position ? rankedPlaces(position, places, radius) : [];
  return <section className="nearby-section" id="gan-ban" aria-labelledby="nearby-title">
    <div className="nearby-heading"><div><span className="section-kicker">KHÁM PHÁ QUANH BẠN</span><h2 id="nearby-title">Địa điểm gần bạn</h2>
      <p>Tự động gợi ý các điểm đến Quận 1 gần vị trí của bạn khi mở trang. Lần đầu hãy chọn Cho phép nếu trình duyệt hỏi quyền vị trí. Tọa độ được xử lý trên thiết bị, không gửi lên máy chủ từ chức năng này.</p></div></div>
    <div className="nearby-controls"><label>Bán kính gợi ý <select value={radius} onChange={event => setRadius(Number(event.target.value))}><option value={1000}>1 km</option><option value={3000}>3 km</option><option value={5000}>5 km</option><option value={10000}>10 km</option></select></label>
      {enabled && <span role="status">{position ? 'Đang tự cập nhật theo vị trí của bạn.' : 'Đang tự lấy vị trí… Hãy cho phép quyền vị trí nếu trình duyệt hỏi.'}</span>}
      <button onClick={enabled ? clear : start}>{enabled ? 'Dừng định vị' : 'Thử lại định vị'}</button></div>
    {error && <p role="alert" className="nearby-message">{error}</p>}
    {position && <><p role="status" className="nearby-message">Tìm thấy {results.length} địa điểm trong {radius / 1000} km · Sai số khoảng {Math.round(position.accuracy)} m · Lúc {new Date(position.timestamp).toLocaleTimeString('vi-VN')}. Khoảng cách là đường thẳng, không phải quãng đường di chuyển.{position.accuracy > 500 && ' GPS chưa chính xác; kết quả chỉ mang tính tham khảo.'}</p>
      {results.length ? <div className="nearby-grid">{results.map(({ place, distance }) => <article className="nearby-card" key={place.id}>
        <div className="nearby-photo"><PlaceImage key={place.imageUrl} place={place} compact /></div><div className="nearby-body"><span className="nearby-distance">Cách khoảng {distance < 1000 ? `${Math.round(distance / 10) * 10} m` : `${(distance / 1000).toFixed(1)} km`}</span><h3>{place.name}</h3><p>{place.address || 'Chưa có địa chỉ xác minh'}</p><button onClick={() => { onSelect(place.id); document.getElementById('chi-tiet')?.scrollIntoView({ behavior: 'smooth' }); }}>Xem & nghe thuyết minh ↗</button></div>
      </article>)}</div> : <p>Không có địa điểm trong bán kính này. Thử tăng bán kính hoặc xem danh sách điểm đến bên dưới.</p>}</>}
  </section>;
}
