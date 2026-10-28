import { useEffect, useState } from 'react';
import { readApi } from './api';

export default function DeviceMonitor() {
  const [page, setPage] = useState(1);
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  useEffect(() => {
    const controller = new AbortController();
    let timer;
    const load = async () => {
      try {
        const result = await readApi(`/admin/devices?page=${page}`, controller.signal);
        if (!controller.signal.aborted) { setData(result); setError(''); }
      } catch (caught) { if (!controller.signal.aborted) setError(caught.message); }
      finally { if (!controller.signal.aborted) timer = setTimeout(load, 15000); }
    };
    load();
    return () => { controller.abort(); clearTimeout(timer); };
  }, [page]);
  const date = value => new Date(value).toLocaleString('vi-VN');
  return <section className="portal-card">
    <h2>Thiết bị truy cập · SQL database</h2>
    <p>Chỉ tính trình duyệt đồng ý thống kê. Hoạt động nghĩa là có tín hiệu trong 5 phút gần nhất; nhiều tab cùng trình duyệt dùng chung mã. Không xác định được số người hoặc thiết bị vật lý chính xác.</p>
    <p className="muted">Dữ liệu tối đa 30 ngày / 10.000 sự kiện gần nhất. Lần đầu hiển thị là lần đầu trong phần dữ liệu còn lưu. Tự cập nhật mỗi 15 giây.</p>
    {error && <p role="alert">Không cập nhật được thống kê: {error}. Dữ liệu bên dưới có thể đã cũ.</p>}
    {!data && !error && <p role="status">Đang đọc dữ liệu SQL…</p>}
    {data && <>
      <div className="metric-grid">
        <div className="metric-card"><strong>{data.online}</strong><span>Thiết bị đang hoạt động</span></div>
        <div className="metric-card"><strong>{data.total}</strong><span>Thiết bị trong dữ liệu còn lưu</span></div>
      </div>
      <p className="muted">Nguồn: {data.storage} · Cập nhật: {date(data.checkedAt)}</p>
      <div className="table-scroll"><table><thead><tr>{['Mã trình duyệt', 'Thiết bị / trình duyệt', 'Trạng thái', 'Ghi nhận đầu', 'Hoạt động cuối', 'Lượt xem', 'Lượt nghe'].map(label => <th key={label}>{label}</th>)}</tr></thead>
        <tbody>{data.items.map(item => <tr key={item.deviceId}>
          <td><code title={item.deviceId}>{item.deviceId.slice(0, 8)}…</code></td>
          <td>{item.descriptor?.operatingSystem || 'Other'} · {item.descriptor?.browser || 'Other'}<small style={{ display: 'block' }}>{item.descriptor?.type || 'Unknown'}</small></td>
          <td>{item.online ? '● Đang hoạt động' : 'Không hoạt động'}</td>
          <td>{date(item.firstSeen)}</td><td>{date(item.lastSeen)}</td><td>{item.views}</td><td>{item.plays}</td>
        </tr>)}</tbody></table></div>
      {!data.items.length && <p>Chưa có thiết bị được ghi nhận trong trang này.</p>}
      <div className="portal-actions"><button disabled={page === 1} onClick={() => setPage(value => value - 1)}>Trang trước</button><span> Trang {page} </span><button disabled={page * data.pageSize >= data.total} onClick={() => setPage(value => value + 1)}>Trang sau</button></div>
    </>}
  </section>;
}
