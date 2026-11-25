import { useEffect, useState } from 'react';
import { docApi, ghiApi } from "../services/dichVuApi.js";
import { layMaTrinhDuyet } from '../utils/dinhDanhTruyCap.js';
import LichSuTamChan from './LichSuTamChan.jsx';

export default function ThietBiTrucTuyen({ canManage = false }) {
  const [blocks, setBlocks] = useState([]);
  const [selected, setSelected] = useState('');
  const [seconds, setSeconds] = useState('30');
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  const [revision, setRevision] = useState(0);
  async function manage(deviceId, unblock = false) {
    const duration = Number(seconds);
    if (!unblock && (!Number.isInteger(duration) || duration < 1 || duration > 86400)) { setMessage('Nhập số nguyên từ 1 đến 86400 giây.'); return; }
    setBusy(true); setMessage('');
    try {
      const result = await ghiApi(unblock ? `/admin/presence/blocks/${deviceId}` : '/admin/presence/blocks', unblock ? 'DELETE' : 'POST', unblock ? {} : { deviceId, seconds: duration });
      setBlocks(previous => unblock ? previous.filter(item => item.deviceId !== deviceId) : [...previous.filter(item => item.deviceId !== deviceId), { deviceId, blockedUntil: result.blockedUntil }]);
      setMessage(unblock ? 'Đã bỏ chặn thiết bị.' : `Đã tạm chặn trong ${duration} giây.`); setRevision(value => value + 1);
    } catch (caught) { setMessage(caught.message); }
    finally { setBusy(false); }
  }
  const [data, setData] = useState(null); const [error, setError] = useState(''); const [page, setPage] = useState(1);
  useEffect(() => {
    const controller = new AbortController(); let timer;
    const load = async () => {
      try {
        const signal = AbortSignal.any([controller.signal, AbortSignal.timeout(10000)]);
        const [result, activeBlocks] = await Promise.all([docApi(`/admin/presence?page=${page}`, signal), canManage ? docApi('/admin/presence/blocks', signal) : Promise.resolve([])]);
        if (!controller.signal.aborted) { setData(result); setBlocks(activeBlocks); setError(''); }
      }
      catch (caught) { if (!controller.signal.aborted) setError(caught.message); }
      /// tải lại sau bao nhiêu giây
      finally { if (!controller.signal.aborted) timer = setTimeout(load, 5000); }
    };
    load(); return () => { controller.abort(); clearTimeout(timer); };
  }, [page, revision, canManage]);
  return <section className="portal-card" aria-label="Thiết bị đang truy cập">
    <h2>Thiết bị đang truy cập website</h2>
    {canManage && <div className="device-block-controls">
      <p>Chặn tạm yêu cầu tới ứng dụng theo mã trình duyệt. Thiết bị nhận thông báo ở lần gọi API tiếp theo, thường trong chu kỳ heartbeat 15 giây. Không chặn nội dung đã tải offline; đổi trình duyệt hoặc xóa mã có thể tránh chặn.</p>
      <label>Chọn thiết bị<select aria-label="Thiết bị cần tạm chặn" value={selected} onChange={event => setSelected(event.target.value)}><option value="">Chọn trong danh sách đang truy cập</option>{(data?.items || []).filter(item => item.deviceId !== layMaTrinhDuyet()).map(item => <option key={item.deviceId} value={item.deviceId}>{item.deviceId.slice(0, 8)} · {item.descriptor.browser} / {item.descriptor.operatingSystem}</option>)}</select></label>
      <label>Thời gian chặn (giây)<input aria-label="Số giây tạm chặn" type="number" min="1" max="86400" step="1" value={seconds} onChange={event => setSeconds(event.target.value)} /></label>
      <button disabled={busy || !selected} onClick={() => manage(selected)}>Tạm chặn thiết bị</button>
      {message && <p role="status">{message}</p>}
      {blocks.length > 0 && <><h3>Đang tạm chặn</h3>{blocks.map(block => <p key={block.deviceId}><code>{block.deviceId.slice(0, 8)}…</code> · Hết chặn {new Date(block.blockedUntil).toLocaleTimeString('vi-VN')} <button disabled={busy} onClick={() => manage(block.deviceId, true)}>Bỏ chặn {block.deviceId.slice(0, 8)}</button></p>)}</>}
    </div>}
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
    {canManage && <LichSuTamChan key={revision} />}
  </section>;
}
