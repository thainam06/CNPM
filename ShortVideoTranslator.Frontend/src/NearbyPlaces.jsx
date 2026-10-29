import { useState } from 'react';
import PlaceImage from './PlaceImage';
import { nearbyPlaces } from './geofence.js';
import './NearbyPlaces.css';

export default function NearbyPlaces({ places, onSelect, location }) {
  const { enabled, position, error, start, clear } = location;
  const [radius, setRadius] = useState(1000);
  const results = enabled ? nearbyPlaces(position, places, radius) : [];
  return <section className="nearby-section" id="gan-ban" aria-labelledby="nearby-title">
    <div className="nearby-heading"><div><span className="section-kicker">KHÁM PHÁ QUANH BẠN</span><h2 id="nearby-title">Địa điểm gần bạn</h2>
      <p>Tự động gợi ý các điểm đến Quận 1 gần vị trí của bạn khi mở trang. Lần đầu hãy chọn Cho phép nếu trình duyệt hỏi quyền vị trí. Tọa độ được xử lý trên thiết bị, không gửi lên máy chủ từ chức năng này.</p></div></div>
    <div className="nearby-controls"><label htmlFor="nearby-radius">Bán kính gợi ý <select id="nearby-radius" value={radius} onChange={event => setRadius(Number(event.target.value))}>{[1, 3, 5, 10].map(km => <option key={km} value={km * 1000}>{km} km</option>)}</select></label>
      <button onClick={enabled ? clear : start}>{enabled ? 'Dừng định vị' : 'Thử lại định vị'}</button></div>
    {error && <p role="alert" className="nearby-message">{error}</p>}
    {!enabled && <p role="status">Định vị đã dừng. Bật lại để cập nhật khoảng cách.</p>}
    {enabled && !position && <p role="status">Đang chờ vị trí mới từ GPS…</p>}
    {enabled && position && position.accuracy > 100 && <p role="status">GPS chưa đủ chính xác (±{Math.round(position.accuracy)} m). Chờ tín hiệu tốt hơn để gợi ý địa điểm.</p>}
    {enabled && position && position.accuracy <= 100 && <>
      <p role="status" className="nearby-message">{results.length ? `${results.length} địa điểm trong ${radius / 1000} km, xếp từ gần đến xa.` : `Không có địa điểm đã xác minh trong ${radius / 1000} km. Thử tăng bán kính hoặc chọn thủ công.`} Sai số khoảng {Math.round(position.accuracy)} m. Khoảng cách là đường thẳng, không phải quãng đường di chuyển.</p>
      <div className="nearby-grid">{results.map(({ place, distance }) => <article className="nearby-card" key={place.id}>
        <div className="nearby-photo"><PlaceImage key={place.imageUrl} place={place} compact /></div><div className="nearby-body"><span className="nearby-distance">Cách khoảng {distance < 1000 ? `${Math.round(distance)} m` : `${(distance / 1000).toFixed(1)} km`}</span><h3>{place.name}</h3><p>{place.address || 'Chưa có địa chỉ xác minh'}</p><button onClick={() => onSelect(place.id)}>Xem & nghe thuyết minh ↗</button></div>
      </article>)}</div>
    </>}
  </section>;
}
