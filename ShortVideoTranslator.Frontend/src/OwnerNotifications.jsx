import { useContext, useEffect, useRef, useState, useSyncExternalStore } from 'react';
import { mutateApi, readApi } from './api';
import { PortalLocale, usePortalText } from './PortalLocale';
import { notificationRoute } from './notificationRoute';

const subscribe = listener => { window.addEventListener('hashchange', listener); return () => window.removeEventListener('hashchange', listener); };
const snapshot = () => window.location.hash;
const date = (value, language) => new Date(value).toLocaleString({ vi: 'vi-VN', en: 'en-GB', zh: 'zh-CN', ja: 'ja-JP', ko: 'ko-KR', fr: 'fr-FR' }[language], { timeZone: 'Asia/Ho_Chi_Minh' });

export function NotificationBell({ actorId, revision, onOpen, onExpired }) {
  const t = usePortalText(); const [record, setRecord] = useState(null); const callbacks = useRef({ onExpired });
  useEffect(() => { callbacks.current = { onExpired }; }, [onExpired]);
  useEffect(() => {
    let active = true; let pending;
    const refresh = async () => {
      if (document.visibilityState === 'hidden') return;
      pending?.abort(); pending = new AbortController(); const signal = pending.signal;
      try { const data = await readApi('/owner/notifications/unread-count', signal); if (active && !signal.aborted) setRecord({ actorId, count: data.unread }); }
      catch (error) { if (active && !signal.aborted) { setRecord({ actorId, error: true }); if (error.status === 401) callbacks.current.onExpired(); } }
    };
    refresh(); const timer = setInterval(refresh, 30000);
    window.addEventListener('online', refresh); document.addEventListener('visibilitychange', refresh);
    return () => { active = false; pending?.abort(); clearInterval(timer); window.removeEventListener('online', refresh); document.removeEventListener('visibilitychange', refresh); };
  }, [actorId, revision]);
  const current = record?.actorId === actorId ? record : null;
  return <div className="notification-bell"><button type="button" onClick={onOpen} aria-label={`${t('Thông báo')}: ${current?.count ?? '?'}`}>
    <span aria-hidden="true">🔔</span> {t('Thông báo')} <strong className="notification-count">{current?.count === undefined ? '…' : current.count > 99 ? '99+' : current.count}</strong>
  </button>{current?.error && <small role="status">{t('Chưa tải được thông báo')}</small>}</div>;
}

export default function OwnerNotifications({ actorId, revision, onChanged, onExpired, onRefreshSession, onRegistration }) {
  const t = usePortalText(); const language = useContext(PortalLocale); const hash = useSyncExternalStore(subscribe, snapshot);
  const route = notificationRoute(hash); const [unreadOnly, setUnreadOnly] = useState(false);
  const [record, setRecord] = useState(null); const [retry, setRetry] = useState(0);
  const [busy, setBusy] = useState(false); const [mutationError, setMutationError] = useState('');
  const pending = useRef(null); const alive = useRef(false);
  const callbacks = useRef({ onExpired });
  useEffect(() => { callbacks.current = { onExpired }; }, [onExpired]);
  useEffect(() => { alive.current = true; return () => { alive.current = false; pending.current?.abort(); }; }, []);
  const key = `${actorId}:${route.id || 'list'}:${route.invalid}:${unreadOnly}:${revision}:${retry}`;
  useEffect(() => {
    if (route.invalid) return;
    const controller = new AbortController();
    const path = route.id ? `/owner/notifications/${encodeURIComponent(route.id)}` : `/owner/notifications?unreadOnly=${unreadOnly}`;
    readApi(path, controller.signal).then(value => { if (!controller.signal.aborted) setRecord({ key, value }); })
      .catch(error => { if (!controller.signal.aborted) { setRecord({ key, error: error.message }); if (error.status === 401) callbacks.current.onExpired(); } });
    return () => controller.abort();
  }, [key, route.id, route.invalid, unreadOnly]);
  const current = record?.key === key ? record : null;
  const detail = route.id ? current?.value : null;
  async function markRead(id) {
    setBusy(true); setMutationError(''); const controller = new AbortController(); pending.current = controller;
    try {
      // mutateApi obtains a fresh CSRF token. Do not optimistically remove an unread notice.
      await mutateApi(`/owner/notifications/${encodeURIComponent(id)}/read`, 'POST', {}, controller.signal);
      if (alive.current && !controller.signal.aborted) { setRetry(value => value + 1); onChanged(); }
    } catch (error) { if (alive.current && !controller.signal.aborted) { setMutationError(error.message); if (error.status === 401) callbacks.current.onExpired(); } }
    finally { if (alive.current && !controller.signal.aborted) setBusy(false); }
  }
  return <section className="owner-notifications" aria-label={t('Thông báo')}>
    {route.id || route.invalid ? <a href="#thong-bao">← {t('Danh sách thông báo')}</a> : <label className="checkbox-label"><input type="checkbox" checked={unreadOnly} onChange={event => setUnreadOnly(event.target.checked)} />{t('Chỉ hiện chưa đọc')}</label>}
    {mutationError && <p role="alert" className="notice">{mutationError}</p>}
    {route.invalid ? <p role="alert" className="notice">{t('Đường dẫn thông báo không hợp lệ')}</p> : !current ? <p role="status">{t('Đang tải dữ liệu…')}</p> : current.error ? <div className="notice" role="alert">{current.error} <button onClick={() => setRetry(value => value + 1)}>{t('Tải lại')}</button></div> : detail ? <article className="portal-card notification-detail">
      <h2>{t('Chi tiết thông báo')}</h2><p className="notification-state">{t(detail.notification.read ? 'Đã đọc' : 'Chưa đọc')} · {date(detail.notification.createdAt, language)}</p>
      <p className="notification-message">{detail.notification.message}</p>
      {detail.review && <><h3>{t(detail.review.kind === 'registration' ? 'Đăng ký chủ quán' : 'Đề xuất')}</h3><p>{t({ approved: 'Đã duyệt', rejected: 'Từ chối', pending: 'Chờ duyệt' }[detail.review.status] || detail.review.status)}</p>
        {detail.review.place && <><h4>{detail.review.place.name}</h4><p className="notification-message">{detail.review.place.description}</p></>}
        {detail.review.adminNote && <div className="notice"><strong>{t('Ghi chú Admin')}</strong><p className="notification-message">{detail.review.adminNote}</p></div>}
        {detail.review.applicantNote && <div><strong>{t('Ghi chú đã gửi Admin')}</strong><p className="notification-message">{detail.review.applicantNote}</p></div>}
      </>}
      {detail.place && <div className="notification-place"><h3>{detail.place.name}</h3><p>{detail.place.address}</p><p>{t(detail.place.isActive ? 'Công khai' : 'Ẩn')}</p></div>}
      <div className="actions">{!detail.notification.read && <button className="primary" disabled={busy} onClick={() => markRead(detail.notification.id)}>{t(busy ? 'Đang xử lý…' : 'Đánh dấu đã đọc')}</button>}
        {detail.review?.kind === 'registration' && detail.review.status === 'approved' && <button disabled={busy} onClick={onRefreshSession}>{t('Cập nhật quyền truy cập')}</button>}
        {detail.review?.kind === 'registration' && detail.review.status === 'rejected' && <button disabled={busy} onClick={onRegistration}>{t('Yêu cầu xem xét lại')}</button>}</div>
    </article> : <>
      <p className="muted">{t('Hiển thị tối đa 100 thông báo gần nhất')}</p>
      {!current.value.length && <p>{t('Chưa có thông báo.')}</p>}
      <div className="notification-list">{current.value.map(item => <article key={item.id} className={`portal-card notification-item ${item.read ? '' : 'unread'}`}>
        <div className="notification-state">{t(item.read ? 'Đã đọc' : 'Chưa đọc')} · {date(item.createdAt, language)}</div>
        <p className="notification-message">{item.message}</p><div className="actions"><a href={`#thong-bao/${encodeURIComponent(item.id)}`}>{t('Xem chi tiết thông báo')}</a>
          {!item.read && <button disabled={busy} onClick={() => markRead(item.id)}>{t('Đánh dấu đã đọc')}</button>}</div>
      </article>)}</div>
    </>}
  </section>;
}
