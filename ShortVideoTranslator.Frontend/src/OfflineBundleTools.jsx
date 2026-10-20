import { useEffect, useRef, useState } from 'react';
import { apiRequest, apiUrl } from './api';
import { offlineGet, commitOfflineBundle } from './offline';
import { installBundle, inspectBundle } from './bundleService';

export default function OfflineBundleTools({ language, shellReady, onWorkingChange }) {
  const [status, setStatus] = useState(null);
  const [working, setWorking] = useState(false);
  const [message, setMessage] = useState('');
  const controller = useRef(null);
  useEffect(() => {
    controller.current?.abort();
    let active = true;
    const inspect = () => inspectBundle(language, offlineGet).then(value => { if (active) setStatus(value); }).catch(() => { if (active) setStatus({ status: 'repair', missing: ['dữ liệu thiết bị'] }); });
    inspect(); window.addEventListener('smarttour-assets-changed', inspect);
    return () => { active = false; controller.current?.abort(); onWorkingChange(false); window.removeEventListener('smarttour-assets-changed', inspect); };
  }, [language, onWorkingChange]);
  async function install() {
    const current = new AbortController(); controller.current = current; setWorking(true); onWorkingChange(true); setMessage('Đang chuẩn bị gói đầy đủ…');
    try {
      await installBundle({ language, request: apiRequest, signal: current.signal, progress: setMessage,
        fetchPhoto: (url, signal) => fetch(url.startsWith('/api/media/') ? apiUrl(url.slice(4)) : url, { signal, credentials: 'omit', referrerPolicy: 'no-referrer' }) }, commitOfflineBundle);
      if (!current.signal.aborted) { setStatus(await inspectBundle(language, offlineGet)); setMessage('Đã kích hoạt gói đầy đủ. Gói trước chỉ được thay sau khi tất cả tài nguyên đã xác minh.'); }
    } catch (error) {
      setMessage(error.name === 'AbortError' ? 'Đã hủy tải; gói đang dùng được giữ.' : error.name === 'QuotaExceededError' ? 'Không đủ dung lượng để lưu gói. Gói đang dùng được giữ; dọn bộ nhớ rồi thử lại.' : error.message);
    } finally { setWorking(false); onWorkingChange(false); }
  }
  return <div className="offline-bundle-tools"><h4>Gói khám phá offline · {language}</h4>
    <p>Lưu cùng lúc bản đồ Quận 1, địa điểm, ảnh, giao diện và MP3 đúng ngôn ngữ. Cần mạng khi tải; chưa có audio tạo sẵn sẽ báo thiếu.</p>
    <div className="tool-actions"><button disabled={working || !shellReady} onClick={install}>{status?.status === 'ready' ? 'Kiểm tra & cập nhật gói đầy đủ' : status?.status === 'repair' ? 'Sửa gói offline' : 'Tải gói offline đầy đủ'}</button>
      {working && <button onClick={() => controller.current?.abort()}>Hủy tải gói</button>}
      <button disabled={working} onClick={async () => setStatus(await inspectBundle(language, offlineGet))}>Kiểm tra gói trên thiết bị</button></div>
    {!shellReady && <p>Chờ giao diện production được lưu trước khi tải gói đầy đủ.</p>}
    {status?.status === 'missing' && <p>Chưa có gói đầy đủ cho ngôn ngữ này.</p>}
    {status?.status === 'repair' && <p className="notice">Gói cần sửa: {status.missing.join(', ')}. Bản cũ còn lại vẫn được giữ.</p>}
    {status?.status === 'ready' && <p className="pwa-ready">✓ Đã lưu {status.meta.places.length} địa điểm · {status.meta.photos.length} ảnh · {status.meta.audio.length} MP3 · bản đồ · {(status.meta.bytes / 1024 / 1024).toFixed(1)} MB.</p>}
    {message && <p role="status" className="notice">{message}</p>}
  </div>;
}
