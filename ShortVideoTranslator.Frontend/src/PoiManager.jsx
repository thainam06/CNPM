import { useState } from 'react';
import PlaceImage from './PlaceImage';
import { filterPois, poiStatus } from './poiCatalog';
import { usePortalText } from './PortalLocale';

const statusLabels = { public: 'Công khai', hidden: 'Ẩn', pending: 'Chờ audio tiếng Anh', archived: 'Lưu trữ Quận 4' };
export default function PoiManager({ places, owner, can, busy, editing, onCreate, onEdit, onToggle, onDelete }) {
  const t = usePortalText();
  const [query, setQuery] = useState('');
  const [status, setStatus] = useState('');
  const [category, setCategory] = useState('');
  const filtered = filterPois(places, query, status, category);
  const categories = [...new Set(places.map(p => p.category))].sort((a, b) => a.localeCompare(b, 'vi'));
  const counts = Object.keys(statusLabels).map(key => [key, places.filter(p => poiStatus(p) === key).length]);
  return <section className="poi-manager" aria-label="Quản lý địa điểm thuyết minh">
    <div className="poi-intro"><div><span className="eyebrow">POINTS OF INTEREST</span><h2>Quản lý địa điểm thuyết minh</h2><p>Biên tập câu chuyện, hình ảnh và vị trí xuất hiện trên trang khám phá.</p></div>{(owner || can('poi:create')) && <button className="primary" disabled={busy || editing} onClick={onCreate}>{owner ? t('+ Đề xuất địa điểm mới') : t('+ Thêm địa điểm')}</button>}</div>
    <div className="poi-statistics" aria-label="Thống kê địa điểm"><button aria-pressed={!status} onClick={() => setStatus('')}><strong>{places.length}</strong><span>Tất cả POI</span></button>{counts.map(([key, count]) => <button key={key} aria-pressed={status === key} onClick={() => setStatus(status === key ? '' : key)}><strong>{count}</strong><span>{t(statusLabels[key])}</span></button>)}</div>
    <div className="poi-toolbar"><label>Tìm địa điểm<input type="search" placeholder="Tên, địa chỉ hoặc mã POI…" value={query} onChange={event => setQuery(event.target.value)} /></label><label>{t('Trạng thái')}<select value={status} onChange={event => setStatus(event.target.value)}><option value="">Tất cả trạng thái</option>{Object.entries(statusLabels).map(([key, label]) => <option key={key} value={key}>{t(label)}</option>)}</select></label><label>{t('Danh mục')}<select value={category} onChange={event => setCategory(event.target.value)}><option value="">Tất cả danh mục</option>{categories.map(item => <option key={item}>{item}</option>)}</select></label></div>
    <div className="poi-results"><span role="status">{filtered.length} / {places.length} địa điểm</span>{(query || status || category) && <button onClick={() => { setQuery(''); setStatus(''); setCategory(''); }}>Xóa bộ lọc ×</button>}</div>
    <div className="table-scroll poi-table"><table><thead><tr>{['Địa điểm', 'Trạng thái', 'Dữ liệu', 'Thao tác'].map(label => <th key={label}>{t(label)}</th>)}</tr></thead><tbody>{filtered.map(place => <tr key={place.id}>
      <td><div className="poi-identity"><div className="poi-thumbnail"><PlaceImage key={place.imageUrl || place.id} place={place} compact /></div><div><small className="poi-code">POI #{place.id} · {place.category}</small><strong>{place.name}</strong><small>{place.address || 'Chưa có địa chỉ'}</small></div></div></td>
      <td><span className={`poi-state poi-state-${poiStatus(place)}`}>{t(statusLabels[poiStatus(place)])}</span><small>{place.isDemo ? 'Dữ liệu demo' : 'Nội dung chính thức'}</small></td>
      <td><span>{place.description.length.toLocaleString('vi-VN')} ký tự thuyết minh</span><small>{place.locationVerified ? '◎ Có vị trí trên bản đồ' : '◎ Chưa xác minh tọa độ'}</small><small>{place.updatedAt ? `Cập nhật ${new Date(place.updatedAt).toLocaleDateString('vi-VN')}` : 'Dữ liệu mẫu'}</small></td>
      <td><div className="poi-row-actions"><button disabled={busy || editing} onClick={() => onEdit(place)}>{owner ? t('Đề xuất sửa') : t('Sửa')}</button>{!owner && can('poi:toggle') && place.region !== 'district4' && <button disabled={busy || editing} onClick={() => onToggle(place)}>{place.isActive || place.activationRequested ? 'Ẩn địa điểm' : 'Công khai'}</button>}{place.isActive && place.region !== 'district4' && <a href={`/?place=${place.id}#chi-tiet`} target="_blank" rel="noreferrer">Xem trên website ↗</a>}{!owner && can('poi:delete') && <button className="danger" disabled={busy || editing} onClick={() => onDelete(place)}>{t('Xóa')}</button>}</div></td>
    </tr>)}</tbody></table>{!filtered.length && <div className="poi-empty"><strong>Không tìm thấy địa điểm</strong><p>{places.length ? 'Thử đổi từ khóa hoặc bỏ bớt bộ lọc.' : 'Thêm địa điểm đầu tiên để bắt đầu biên tập.'}</p></div>}</div>
  </section>;
}
