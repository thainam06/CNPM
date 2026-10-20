import { useState } from 'react';
import { mutateApi } from './api';
import { usePortalText } from './PortalLocale';

export default function RegistrationStatus({ data, busy, run }) {
  const t = usePortalText(); const [note, setNote] = useState('');
  const reviews = data?.reviews || [];
  const canResubmit = data && !data.user.verified && reviews.some(r => r.status === 'rejected') && !reviews.some(r => r.status === 'pending');
  return <section className="portal-card"><h2>{t('Trạng thái đăng ký chủ quán')}</h2>
    <p>{t('Admin cần xác minh chủ quán trước khi bạn quản lý địa điểm.')}</p>
    {!data && <p role="status">{t('Đang tải dữ liệu…')}</p>}
    {reviews.map(review => <div className="registration-history" key={review.id}><strong>{t({ pending: 'Chờ duyệt', approved: 'Đã duyệt', rejected: 'Từ chối' }[review.status] || review.status)}</strong>
      {review.applicantNote && <p className="notification-message">{t('Ghi chú đã gửi Admin')}: {review.applicantNote}</p>}
      {review.adminNote && <p className="notification-message">{t('Ghi chú Admin')}: {review.adminNote}</p>}</div>)}
    {canResubmit && <form onSubmit={async event => { event.preventDefault(); const outcome = await run(() => mutateApi('/owner/registration-resubmission', 'POST', { note }), 'Yêu cầu đã gửi, chờ Admin xem xét.'); if (outcome) setNote(''); }}>
      <h3>{t('Yêu cầu xem xét lại')}</h3><label>{t('Ghi chú gửi Admin')}<textarea required rows={4} maxLength={1000} value={note} onChange={event => setNote(event.target.value)} /></label>
      <button className="primary" disabled={busy || !note.trim()}>{t('Gửi yêu cầu xem xét lại')}</button>
    </form>}
  </section>;
}
