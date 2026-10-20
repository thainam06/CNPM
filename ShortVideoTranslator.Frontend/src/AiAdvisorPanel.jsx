import { useEffect, useRef, useState } from 'react';
import { apiRequest, readApi } from './api';

export default function AiAdvisorPanel({ place, disabled, onApply }) {
  const [usage, setUsage] = useState(null);
  const [error, setError] = useState('');
  const [draftRecord, setDraft] = useState(null);
  const [reviewed, setReviewed] = useState(false);
  const [runningSource, setWorking] = useState(null);
  const [useWeather, setUseWeather] = useState(false);
  const [retry, setRetry] = useState(0);
  const pending = useRef(null);
  const source = JSON.stringify({ text: place.description, name: place.name, category: place.category, address: place.address || null, placeId: place.id || null, isDemo: place.isDemo });
  const draft = draftRecord?.source === source ? draftRecord.value : null;
  const working = runningSource?.source === source && !runningSource.controller.signal.aborted;
  useEffect(() => {
    const controller = new AbortController();
    readApi('/ai/usage', controller.signal).then(value => { if (!controller.signal.aborted) { setUsage(value); setError(''); } }).catch(caught => { if (!controller.signal.aborted) { setUsage(null); setError(caught.message); } });
    return () => controller.abort();
  }, [retry]);
  useEffect(() => () => pending.current?.abort(), [source]);
  async function generate() {
    const controller = new AbortController(); pending.current?.abort(); pending.current = controller;
    setWorking({ source, controller }); setError(''); setDraft(null); setReviewed(false);
    try {
      const csrf = await readApi('/auth/csrf', controller.signal);
      const response = await apiRequest('/ai/enhance-description', { method: 'POST', signal: controller.signal,
        headers: { 'Content-Type': 'application/json', 'X-CSRF-Token': csrf.token }, body: JSON.stringify({ ...JSON.parse(source), useWeather }) });
      const result = await response.json();
      if (!controller.signal.aborted) setDraft({ source, value: result });
    } catch (caught) { if (!controller.signal.aborted) setError(caught.message); }
    finally {
      if (!controller.signal.aborted) {
        setWorking(null);
        readApi('/ai/usage', controller.signal).then(value => { if (!controller.signal.aborted) setUsage(value); }).catch(() => {});
      }
    }
  }
  const quota = usage?.quota;
  const weatherPossible = usage?.weatherReady && place.id > 0 && place.locationVerified && place.latitude !== null && place.longitude !== null;
  return <section className="ai-advisor" aria-label="Trợ lý AI biên tập">
    <h3>Trợ lý AI biên tập</h3>
    <p>AI chỉ đề xuất bản nháp từ thông tin đã nhập. Bạn kiểm tra tên riêng, địa chỉ và dữ kiện trước khi dùng; nội dung không tự lưu hoặc công khai.</p>
    {!usage && !error && <p role="status">Đang kiểm tra cấu hình AI…</p>}
    {usage && <>
      <p className={usage.ready ? 'muted' : 'notice'}>{usage.ready ? `Đã cấu hình ${usage.provider}. Kết nối được kiểm tra khi tạo bản nháp.` : 'AI chưa sẵn sàng: chưa cấu hình nhà cung cấp ở backend. Bạn vẫn có thể biên tập thủ công.'}</p>
      <p className="muted">{quota.limit === null ? `Admin không giới hạn lượt; đã tạo ${quota.used} bản nháp hôm nay.` : `Còn ${quota.remaining}/${quota.limit} lượt; đã tạo ${quota.used}, đang xử lý ${quota.inFlight}. Đặt lại lúc ${new Date(quota.resetsAt).toLocaleString('vi-VN')}.`} Chỉ tính lượt khi máy chủ tạo xong bản nháp; lỗi hoặc hủy trước khi hoàn tất không trừ lượt.</p>
      <label className="checkbox-label"><input type="checkbox" checked={useWeather} disabled={!weatherPossible || working || disabled} onChange={event => setUseWeather(event.target.checked)} />Thêm ngữ cảnh thời tiết OpenWeather (tùy chọn)</label>
      {!weatherPossible && <small>Thời tiết chưa sẵn sàng: cần dịch vụ được cấu hình và địa điểm đã có tọa độ xác minh. Không dùng vị trí của khách.</small>}
    </>}
    {error && <p role="alert" className="notice">{error}</p>}
    <div className="actions"><button type="button" disabled={disabled || working || !usage?.ready || quota?.remaining === 0 || !place.description.trim()} onClick={generate}>{working ? 'Đang tạo bản nháp AI…' : 'Tạo bản nháp AI'}</button>
      {working && <button type="button" onClick={() => { pending.current?.abort(); setWorking(null); setError('Đã gửi hủy. Nếu máy chủ đã tạo xong bản nháp trước đó, lượt thành công vẫn được tính.'); setRetry(value => value + 1); }}>Hủy yêu cầu AI</button>}
      <button type="button" disabled={working || disabled} onClick={() => setRetry(value => value + 1)}>Kiểm tra lại cấu hình AI</button></div>
    {draft && <div className="ai-preview"><p>Nhà cung cấp thực tế: <strong>{draft.provider}</strong> · Bản nháp cần rà soát</p>
      {draft.warnings.map((warning, index) => <p className="notice" key={index}>{warning}</p>)}
      {draft.weather && <p>OpenWeather: {draft.weather.temperatureC}°C, {draft.weather.description}; quan trắc {new Date(draft.weather.observedAt).toLocaleString('vi-VN')}. Không coi là dự báo cho ngày tham quan.</p>}
      <div className="form-grid"><div><h4>Nguồn đang biên tập</h4><p>{place.description}</p></div><div><h4>Đề xuất của AI</h4><p>{draft.text}</p></div></div>
      <label className="checkbox-label"><input type="checkbox" checked={reviewed} onChange={event => setReviewed(event.target.checked)} />Tôi đã rà soát dữ kiện và chấp nhận bản nháp này</label>
      <div className="actions"><button type="button" className="primary" disabled={!reviewed || disabled} onClick={() => { onApply(draft.text); setDraft(null); setReviewed(false); }}>Đưa vào ô thuyết minh</button><button type="button" onClick={() => { setDraft(null); setReviewed(false); }}>Bỏ đề xuất AI</button></div>
    </div>}
  </section>;
}
