import { useState } from 'react';
import { tinhKhoangCachMet } from "./vungKichHoat.js";
import "./DoKhoangCach.css";

const empty = () => ({ latitude: '', longitude: '' });
const valid = p => Number.isFinite(p.latitude) && Math.abs(p.latitude) <= 90 && Number.isFinite(p.longitude) && Math.abs(p.longitude) <= 180;

export default function DoKhoangCach({ places }) {
  const [points, setPoints] = useState([empty(), empty()]);
  const [selections, setSelections] = useState(['', '']);
  const [result, setResult] = useState(null);
  const [error, setError] = useState('');
  const available = places.filter(p => p.locationVerified && valid(p));
  function change(index, next, selection = '') {
    setPoints(current => current.map((p, i) => i === index ? next : p));
    setSelections(current => current.map((id, i) => i === index ? selection : id));
    setResult(null); setError('');
  }
  function calculate(event) {
    event.preventDefault();
    const parsed = points.map(p => ({ latitude: Number(p.latitude), longitude: Number(p.longitude) }));
    if (points.some(p => p.latitude.trim() === '' || p.longitude.trim() === '') || parsed.some(p => !valid(p))) {
      setError('Nhập đủ tọa độ: vĩ độ từ −90 đến 90, kinh độ từ −180 đến 180.'); setResult(null); return;
    }
    setError(''); setResult(tinhKhoangCachMet(parsed[0], parsed[1]));
  }
  return <section className="distance-calculator" id="do-khoang-cach" aria-labelledby="distance-title">
    <div className="distance-heading"><div><span className="section-kicker">LÊN KẾ HOẠCH KHÁM PHÁ</span><h2 id="distance-title">Đo khoảng cách giữa hai điểm</h2></div><span aria-hidden="true">↔</span></div>
    <p>Chọn địa điểm có sẵn hoặc nhập tọa độ bất kỳ. Tính ngay trên thiết bị, không cần gửi tọa độ lên máy chủ.</p>
    <form onSubmit={calculate}>
      <div className="distance-points">{points.map((point, index) => <fieldset key={index}>
        <legend>Điểm {index === 0 ? 'A · Điểm đầu' : 'B · Điểm cuối'}</legend>
        <label>Địa điểm có sẵn<select value={selections[index]} onChange={event => {
          const place = available.find(p => String(p.id) === event.target.value);
          change(index, place ? { latitude: String(place.latitude), longitude: String(place.longitude) } : empty(), event.target.value);
        }}><option value="">Nhập tọa độ tùy chọn</option>{available.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}</select></label>
        <div className="distance-coordinates">
          <label>Vĩ độ<input type="number" step="any" min="-90" max="90" required placeholder="Ví dụ: 10.77" value={point.latitude} onChange={e => change(index, { ...point, latitude: e.target.value })} /></label>
          <label>Kinh độ<input type="number" step="any" min="-180" max="180" required placeholder="Ví dụ: 106.70" value={point.longitude} onChange={e => change(index, { ...point, longitude: e.target.value })} /></label>
        </div>
      </fieldset>)}</div>
      <div className="distance-actions"><button className="primary" type="submit">Tính khoảng cách</button><button type="button" onClick={() => { setPoints([empty(), empty()]); setSelections(['','']); setResult(null); setError(''); }}>Xóa tọa độ</button></div>
    </form>
    {error && <p className="notice" role="alert">{error}</p>}
    {result !== null && <output className="distance-result" aria-live="polite"><span>Khoảng cách A ↔ B</span><strong>{result >= 1000 ? `${(result / 1000).toLocaleString('vi-VN', { maximumFractionDigits: 2 })} km` : `${result.toLocaleString('vi-VN', { maximumFractionDigits: 1 })} m`}</strong></output>}
    <small>Ước tính bằng công thức Haversine trên bề mặt Trái Đất. Không phải quãng đường theo đường giao thông hoặc thời gian di chuyển.</small>
  </section>;
}
