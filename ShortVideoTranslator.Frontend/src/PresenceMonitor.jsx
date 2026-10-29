import { useEffect, useState } from 'react';
import { readApi } from './api';

export default function PresenceMonitor() {
  const [data, setData] = useState(null); const [error, setError] = useState(''); const [page, setPage] = useState(1);
  useEffect(() => {
    const controller = new AbortController(); let timer;
    const load = async () => {
      try { const result = await readApi(`/admin/presence?page=${page}`, AbortSignal.any([controller.signal, AbortSignal.timeout(10000)])); if (!controller.signal.aborted) { setData(result); setError(''); } }
      catch (caught) { if (!controller.signal.aborted) setError(caught.message); }
      /// tải lại sau bao nhiêu giây
      finally { if (!controller.signal.aborted) timer = setTimeout(load, 5000); }
    };
    load(); return () => { controller.abort(); clearTimeout(timer); };
  }, [page]);
  return <section className="portal-card" aria-label="Thiết bị đang truy cập">
    <h2>Thiết bị đang truy cập website</h2>
    <p>Tự ghi nhận khi mở web, không cần bật thống kê lượt xem/nghe. Một mã đại diện cho một trình duyệt; nhiều tab cùng trình duyệt tính một. Không thu thập tên máy, địa chỉ MAC hoặc GPS ở đây.</p>
    <p className="muted">Chỉ hiện trình duyệt đang hoạt động · Cập nhật mỗi 5 giây. Thoát hoặc ẩn ứng dụng sẽ gửi tín hiệu rời trang. Nếu bị mất mạng/tắt đột ngột, phiên tự hết hạn sau 45 giây.</p>
    {error && <p role="alert">Không cập nhật được: {error}. Dữ liệu cũ có thể không còn chính xác.</p>}
    {!data && !error && <p role="status">Đang tải thiết bị…</p>}
    {data && !error && <>
      <div className="metric-grid"><div className="metric-card"><strong>{data.online}</strong><span>Thiết bị đang truy cập</span></div></div>
      <p className="muted">Cập nhật: {new Date(data.checkedAt).toLocaleString('vi-VN')}</p>
      <div className="table-scroll"><table><thead><tr>{['Mã trình duyệt', 'Hệ điều hành', 'Trình duyệt', 'Loại', 'Hoạt động gần nhất'].map(label => <th key={label}>{label}</th>)}</tr></thead><tbody>
        {data.items.map(item => <tr key={item.deviceId}><td><code title={item.deviceId}>{item.deviceId.slice(0, 8)}…</code></td><td>{item.descriptor.operatingSystem}</td><td>{item.descriptor.browser}</td><td>{item.descriptor.type}</td><td>{new Date(item.lastSeen).toLocaleString('vi-VN')}</td></tr>)}
      </tbody></table></div>
      {!data.items.length && <p>Không có thiết bị đang hoạt động trong trang này.</p>}
      <button disabled={page <= 1} onClick={() => setPage(value => value - 1)}>Trang trước</button> <span>Trang {page}</span> <button disabled={page * data.pageSize >= data.online} onClick={() => setPage(value => value + 1)}>Trang sau</button>
    </>}
  </section>;
}
