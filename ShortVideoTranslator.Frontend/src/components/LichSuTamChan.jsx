import { useEffect, useState } from 'react';
import { docApi } from '../services/dichVuApi.js';

const thoiDiem = value => value ? new Date(value).toLocaleString('vi-VN') : 'Chưa ghi nhận';

export default function LichSuTamChan() {
  const [page, setPage] = useState(1);
  const [revision, setRevision] = useState(0);
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  useEffect(() => {
    const controller = new AbortController();
    docApi(`/admin/presence/blocks/history?page=${page}&pageSize=20`,
      AbortSignal.any([controller.signal, AbortSignal.timeout(15000)]))
      .then(value => { if (!controller.signal.aborted) { setData(value); setError(''); } })
      .catch(caught => { if (!controller.signal.aborted) setError(caught.message); });
    return () => controller.abort();
  }, [page, revision]);
  function reload(nextPage = page) {
    setData(null); setError(''); setPage(nextPage); setRevision(value => value + 1);
  }
  return <section aria-label="Lịch sử tạm chặn thiết bị">
    <h3>Lịch sử tạm chặn thiết bị</h3>
    <p className="muted">Nhật ký chặn và bỏ chặn, mới nhất trước. Tự tải lại sau thao tác trên trang này; bấm cập nhật để xem thao tác từ quản trị viên khác. Lịch sử dùng chung giới hạn 5.000 bản ghi nhật ký hệ thống. Tự hết hạn không tạo thao tác bỏ chặn.</p>
    <button onClick={() => reload(1)}>Cập nhật lịch sử</button>
    {error && <p role="alert">Không tải được lịch sử: {error}</p>}
    {!data && !error && <p role="status">Đang tải lịch sử…</p>}
    {data && <>
      <p>{data.total} thao tác được lưu</p>
      <div className="table-scroll"><table>
        <thead><tr>{['Thời điểm', 'Mã trình duyệt', 'Thao tác', 'Người thực hiện', 'Thời lượng', 'Hạn chặn đã đặt'].map(label => <th key={label}>{label}</th>)}</tr></thead>
        <tbody>{data.items.map(item => <tr key={item.id}>
          <td>{thoiDiem(item.createdAt)}</td>
          <td><code title={item.deviceId}>{item.deviceId}</code></td>
          <td>{item.action === 'device.block' ? 'Tạm chặn' : 'Bỏ chặn'}</td>
          <td title={item.actorId}>{item.actorName}</td>
          <td>{item.action === 'device.unblock' ? '—' : item.seconds == null ? 'Chưa ghi nhận' : `${item.seconds} giây`}</td>
          <td>{thoiDiem(item.blockedUntil)}</td>
        </tr>)}</tbody>
      </table></div>
      {!data.items.length && <p>Không có thao tác trong trang này.</p>}
      <button disabled={page <= 1} onClick={() => reload(page - 1)}>Lịch sử trước</button>
      <span> Trang {data.page} / {Math.max(1, Math.ceil(data.total / data.pageSize))} </span>
      <button disabled={page * data.pageSize >= data.total} onClick={() => reload(page + 1)}>Lịch sử sau</button>
    </>}
  </section>;
}
