import { ngonNguBanDich } from '../data/danhSachNgonNgu.js';
import { useEffect, useRef, useState } from 'react';
import { useChuQuanTri } from './NgonNguQuanTri.jsx';
import { guiYeuCauApi, diaChiApi, ghiApi, docApi } from "../services/dichVuApi.js";


export default function QuanLyBanDich({ data, revision, busy, can, run }) {
  const t = useChuQuanTri();
  const [placeId, setPlaceId] = useState(data.places[0]?.id || '');
  const [language, setLanguage] = useState('en');
  const [view, setView] = useState(null);
  const [text, setText] = useState('');
  const [error, setError] = useState('');
  const [retry, setRetry] = useState(0);
  const [previewing, setPreviewing] = useState(false);
  const pending = useRef(null);
  useEffect(() => {
    if (!placeId) return;
    const controller = new AbortController(); pending.current?.abort();
    docApi(`/admin/translations/${placeId}/${language}`, controller.signal).then(value => {
      if (controller.signal.aborted) return;
      setView(value); setText(value.draft?.text || value.audio?.text || ''); setError('');
    }).catch(caught => { if (!controller.signal.aborted) { setView(null); setError(caught.message); } });
    return () => { controller.abort(); pending.current?.abort(); };
  }, [placeId, language, revision, retry]);
  function choose(setter, value) { pending.current?.abort(); setView(null); setText(''); setError(''); setPreviewing(false); setter(value); }
  async function preview() {
    const controller = new AbortController(); pending.current = controller; setPreviewing(true); setError('');
    try {
      const result = await (await guiYeuCauApi('/narration/translate', { method: 'POST', signal: controller.signal, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ text: view.place.description, language }) })).json();
      if (!controller.signal.aborted) setText(result.text);
    } catch (caught) { if (!controller.signal.aborted) setError(caught.message); }
    finally { if (!controller.signal.aborted) setPreviewing(false); }
  }
  const draft = view?.draft;
  const unsaved = !draft || text.trim() !== draft.text;
  const stale = draft && draft.sourceHash !== view?.sourceHash;
  const blocked = busy || previewing;
  return <section className="portal-card"><h2>{t("Biên tập và duyệt bản dịch thuyết minh")}</h2>
    <p>{t("Bản nháp không thay nội dung khách đang nghe. Khi duyệt, hệ thống tạo MP3 đúng ngôn ngữ trước; chỉ công khai khi audio sẵn sàng. Bản dịch áp dụng cho đúng nội dung gốc, kể cả địa điểm dùng chung nội dung đó.")}</p>
    <div className="form-grid"><label>{t('Địa điểm')}<select value={placeId} disabled={blocked} onChange={event => choose(setPlaceId, Number(event.target.value))}>{data.places.map(place => <option key={place.id} value={place.id}>{place.name}{place.isActive ? '' : ` · ${t('Ẩn')}`}</option>)}</select></label>
      <label>{t("Ngôn ngữ bản dịch")}<select value={language} disabled={blocked} onChange={event => choose(setLanguage, event.target.value)}>{ngonNguBanDich.map(({code, name}) => <option key={code} value={code}>{name}</option>)}</select></label></div>
    {!data.places.length && <p>{t("Chưa có địa điểm Quận 1.")}</p>}
    {error && <p className="notice" role="alert">{error}</p>}
    {placeId && !view && !error && <p role="status">{t("Đang tải bản dịch…")}</p>}
    <button disabled={blocked} onClick={() => setRetry(value => value + 1)}>{t("Tải lại bản dịch")}</button>
    {view && <>
      <div className="form-grid"><label>{t("Nội dung gốc tiếng Việt")}<textarea rows={8} readOnly value={view.place.description} /></label><label>{t("Bản dịch cần rà soát")}<textarea rows={8} maxLength={3000} lang={language} value={text} disabled={blocked} onChange={event => setText(event.target.value)} /><small>{text.length}/3000 {t('ký tự')}</small></label></div>
      {stale && <p className="notice">{t("Nguồn tiếng Việt đã đổi. Rà soát lại bản dịch và lưu bản nháp mới trước khi duyệt.")}</p>}
      <p className="muted">{view.published ? t("Đang công khai bản dịch đã duyệt.") : t("Chưa có bản dịch do Admin duyệt; khách dùng bản dịch tự động khi dịch vụ sẵn sàng.")} {draft?.status === 'approved' && !stale ? t("Bản nháp gần nhất đã được duyệt.") : draft ? t("Có bản nháp đang chờ rà soát.") : ''}</p>
      <div className="actions"><button disabled={blocked} onClick={preview}>{previewing ? t("Đang lấy bản dịch…") : t("Lấy bản dịch hiện tại để biên tập")}</button>
        <button disabled={blocked || !text.trim()} onClick={() => run(() => ghiApi(`/admin/translations/${placeId}/${language}`, 'PUT', { text, sourceHash: view.sourceHash, expectedVersion: draft?.version ?? null }), 'Đã lưu bản nháp; nội dung công khai chưa thay đổi.')}>{t("Lưu bản nháp")}</button>
        {can('content:publish') && can('content:moderate') && <button className="primary" disabled={blocked || unsaved || stale || draft?.status !== 'draft'} onClick={() => run(() => ghiApi(`/admin/translations/${placeId}/${language}/approve`, 'POST', { version: draft.version }), 'Đã duyệt bản dịch và tạo MP3. Khách tải lại danh sách để nhận phiên bản mới.')}>{busy ? t("Đang xử lý…") : t("Duyệt & tạo MP3")}</button>}
        {view.published && can('content:publish') && can('content:moderate') && <button className="danger" disabled={blocked} onClick={() => run(() => ghiApi(`/admin/translations/${placeId}/${language}/withdraw`, 'POST', { version: view.published.revision }), 'Đã thu hồi bản dịch đã duyệt. Hệ thống trở về luồng dịch tự động.')}>{t("Thu hồi bản đang công khai")}</button>}
      </div>
      {unsaved && <small>{t("Lưu bản nháp trước khi duyệt. Kiểm tra tên riêng, địa chỉ và ý nghĩa; không giữ nguyên tiếng Việt để đọc bằng giọng ngoại ngữ.")}</small>}
      {view.audio && <section className="translation-audio"><h3>{t("Thuyết minh đang dùng")}</h3><p lang={language}>{view.audio.text}</p><audio controls preload="none" src={diaChiApi(view.audio.audioUrl.replace(/^\/api/, ''))} aria-label={t('Nghe bản dịch đang dùng')} /></section>}
    </>}
  </section>;
}
