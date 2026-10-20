import { useEffect, useState } from 'react';
import { apiUrl, baseUrl, mutateApi, readApi } from './api';
import './App.css';
import './Portal.css';
import TranslationPanel from './TranslationPanel';
import AiAdvisorPanel from './AiAdvisorPanel';
import { PortalLocale, usePortalText } from './PortalLocale';
import { portalLanguages } from './portalMessages';
import OwnerNotifications, { NotificationBell } from './OwnerNotifications';
import { notificationRoute } from './notificationRoute';
import RegistrationStatus from './RegistrationStatus';

const labels = { translations: 'Bản dịch thuyết minh', overview: 'Tổng quan', places: 'Địa điểm', users: 'Tài khoản', roles: 'Vai trò', reviews: 'Kiểm duyệt', audio: 'Tác vụ audio', audit: 'Nhật ký', submissions: 'Đề xuất', notifications: 'Thông báo', runtime: 'Giám sát vị trí', system: 'Kiểm tra hệ thống', identity: 'Xác minh chủ quán', password: 'Mật khẩu' };
const permissions = { translations: 'poi:update', overview: 'analytics:view', places: 'poi:update', users: 'user:read', roles: 'role:read', reviews: 'content:moderate', audio: 'poi:update', audit: 'audit:read', runtime: 'system:logs', system: 'system:logs' };
const statuses = { pending: 'Chờ duyệt', approved: 'Đã duyệt', rejected: 'Từ chối', queued: 'Đang chờ', running: 'Đang tạo', paused: 'Tạm dừng', completed: 'Hoàn tất', failed: 'Thất bại', cancelled: 'Đã hủy' };
const blankPlace = { region: 'district1', id: 0, name: '', category: 'Ẩm thực', description: '', address: '', latitude: '', longitude: '', sourceUrl: '', isDemo: true, isActive: false, locationVerified: false, imageUrl: '', imageNote: '', ownerId: '', triggerRadius: 30, audioPriority: 0, updatedAt: null };

function Table({ headings, children }) { const t = usePortalText(); return <div className="table-scroll"><table><thead><tr>{headings.map(heading => <th key={heading}>{t(heading)}</th>)}</tr></thead><tbody>{children}</tbody></table></div>; }
function Badge({ status }) { const t = usePortalText(); return <span className={`badge badge-${status}`}>{t(statuses[status] || status)}</span>; }

export default function Portal() {
  const [language, setLanguage] = useState(() => portalLanguages.includes(localStorage.getItem('smarttour-portal-language')) ? localStorage.getItem('smarttour-portal-language') : 'vi');
  return <PortalLocale.Provider value={language}><PortalContent language={language} onLanguage={value => { localStorage.setItem('smarttour-portal-language', value); setLanguage(value); }} /></PortalLocale.Provider>;
}
function PortalContent({ language, onLanguage }) { const t = usePortalText();
  const owner = window.location.pathname.startsWith('/owner');
  const [user, setUser] = useState(null);
  const [initializing, setInitializing] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(false);
  const [selectedTab, setTab] = useState(owner ? notificationRoute(window.location.hash).inbox ? 'notifications' : 'places' : 'overview');
  const tab = owner || !user || selectedTab === 'password' || user.permissions.includes(permissions[selectedTab])
    ? selectedTab : Object.keys(permissions).find(key => user.permissions.includes(permissions[key])) || 'password';
  const [data, setData] = useState(null);
  const [revision, setRevision] = useState(0);
  const [runtimeHours, setRuntimeHours] = useState(1);
  const [editor, setEditor] = useState(null);
  const [owners, setOwners] = useState([]);
  const [roles, setRoles] = useState([]);
  const [registration, setRegistration] = useState(false);
  const [credentials, setCredentials] = useState({ username: '', password: '', displayName: '' });
  const can = (permission) => user?.permissions?.includes(permission);

  useEffect(() => {
    if (!owner) return;
    const navigate = () => {
      if (notificationRoute(window.location.hash).inbox) { setTab('notifications'); setData(null); setEditor(null); setError(''); setNotice(''); }
    };
    window.addEventListener('hashchange', navigate);
    return () => window.removeEventListener('hashchange', navigate);
  }, [owner]);

  useEffect(() => {
    const controller = new AbortController();
    readApi('/auth/me', controller.signal).then(setUser).catch(caught => {
      if (!controller.signal.aborted && caught.status !== 401) setError(caught.message);
    }).finally(() => { if (!controller.signal.aborted) setInitializing(false); });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    if (!user || user.mustChangePassword || tab === 'password' || (owner && tab === 'notifications')) return;
    const controller = new AbortController();
    const endpoints = owner ? { places: '/owner/places', overview: '/owner/analytics', submissions: '/owner/submissions', notifications: '/owner/notifications', identity: '/owner/identity' }
      : { translations: '/admin/translations', overview: '/admin/analytics', places: '/admin/places', users: '/admin/users', roles: '/admin/roles', reviews: '/admin/reviews', audio: '/admin/audio-tasks', audit: '/admin/audit', runtime: `/runtime/window?hours=${runtimeHours}`, system: '/admin/system-status' };
    const path = owner && !user.verified && tab !== 'identity' ? '/owner/registration-status' : endpoints[tab];
    if (!path) return;
    readApi(path, controller.signal).then(setData).catch(caught => {
      if (!controller.signal.aborted) { setError(caught.message); if (caught.status === 401) setUser(null); }
    });
    if (!owner && user.permissions.includes('user:read') && (tab === 'users' || tab === 'places')) {
      readApi('/admin/assignable-roles', controller.signal).then(setRoles).catch(() => {});
      readApi('/admin/users', controller.signal).then(items => setOwners(items.filter(item => item.role === 'poi_owner' && item.verified && item.enabled))).catch(() => {});
    }
    return () => controller.abort();
  }, [user, tab, revision, owner, runtimeHours]);

  useEffect(() => {
    if (!user || owner || tab !== 'audio' || user.mustChangePassword || !user.permissions.includes('poi:update')) return;
    const events = new EventSource(apiUrl('/admin/audio-tasks/stream'), { withCredentials: true });
    events.onmessage = event => { try { setData(JSON.parse(event.data)); } catch { setError('Không đọc được cập nhật tác vụ.'); } };
    events.onerror = () => { setError('Kết nối cập nhật audio bị gián đoạn. Tải lại để thử kết nối.'); events.close(); };
    return () => events.close();
  }, [user, tab, owner]);

  async function run(action, message = 'Đã lưu thay đổi.') {
    setBusy(true); setError(''); setNotice('');
    try { const result = await action(); setNotice(message); setRevision(value => value + 1); return { result }; }
    catch (caught) { setError(caught.message); if (caught.status === 401) setUser(null); return null; }
    finally { setBusy(false); }
  }
  function selectTab(value) {
    setTab(value); setData(null); setEditor(null); setError(''); setNotice('');
    if (owner) {
      if (value === 'notifications') window.location.hash = 'thong-bao';
      else if (notificationRoute(window.location.hash).inbox) window.location.hash = '';
    }
  }
  async function login(event) {
    event.preventDefault();
    const outcome = await run(() => mutateApi(registration ? '/auth/register-owner' : '/auth/login', 'POST', credentials), registration ? 'Đăng ký thành công. Đăng nhập để theo dõi kết quả duyệt.' : 'Đăng nhập thành công.');
    if (outcome) {
      setCredentials({ username: '', password: '', displayName: '' });
      if (registration) setRegistration(false); else { setUser(outcome.result); setData(null); }
    }
  }
  const allowedTabs = owner ? ['places', 'submissions', 'notifications', 'identity', 'overview', 'password'] : Object.keys(labels).filter(key => key === 'password' || can(permissions[key]));

  return <div className="portal">
    <header className="portal-header"><a href="/" className="portal-brand">📍 SmartTour Guide</a><span>{owner ? t('Cổng chủ quán') : t('Quản trị hệ thống')}</span><a href="/">{t('← Trang khám phá')}</a>{owner && user && !user.mustChangePassword && <NotificationBell key={user.id} actorId={user.id} revision={revision} onOpen={() => selectTab('notifications')} onExpired={() => setUser(null)} />}<label className="portal-language">{t('Ngôn ngữ')}<select aria-label="Portal language" value={language} onChange={event => onLanguage(event.target.value)}>{[['vi','Tiếng Việt'],['en','English'],['zh','中文'],['ja','日本語'],['ko','한국어'],['fr','Français']].map(([code,name]) => <option key={code} value={code}>{name}</option>)}</select></label></header>
    {language !== 'vi' && <p className="portal-locale-note">{{ en: 'Navigation, forms and common actions are translated. Source content, operational notes and server messages remain in Vietnamese.', zh: '导航、表单和常用操作已翻译。源内容、操作说明和服务器消息保留越南语。', ja: 'ナビゲーション、フォーム、基本操作は翻訳済みです。元の内容、運用メモ、サーバーメッセージはベトナム語です。', ko: '탐색, 양식 및 공통 작업이 번역되었습니다. 원본 콘텐츠, 운영 안내 및 서버 메시지는 베트남어로 유지됩니다.', fr: 'La navigation, les formulaires et les actions courantes sont traduits. Le contenu source, les notes techniques et les messages du serveur restent en vietnamien.' }[language]}</p>}
    {!user ? <main className="login-card"><span className="eyebrow">SMARTTOUR PORTAL</span><h1>{registration ? t('Đăng ký chủ quán') : t('Đăng nhập')}</h1>
      <p>{owner ? t('Quản lý thông tin quán và gửi nội dung để Admin duyệt.') : t('Dành cho tài khoản có quyền quản trị.')}</p>
      {initializing ? <p role="status">{t('Đang kiểm tra phiên đăng nhập…')}</p> : <form onSubmit={login}>
        <label>{t('Tên đăng nhập')}<input required autoComplete="username" value={credentials.username} onChange={event => setCredentials({ ...credentials, username: event.target.value })} maxLength={80} /></label>
        {registration && <label>{t('Tên hiển thị')}<input required value={credentials.displayName} onChange={event => setCredentials({ ...credentials, displayName: event.target.value })} maxLength={100} /></label>}
        <label>{t('Mật khẩu')}<input type="password" required autoComplete={registration ? 'new-password' : 'current-password'} minLength={registration ? 7 : 1} maxLength={128} value={credentials.password} onChange={event => setCredentials({ ...credentials, password: event.target.value })} /></label>
        <button className="primary" disabled={busy}>{busy ? t('Đang xử lý…') : registration ? t('Gửi đăng ký') : t('Đăng nhập')}</button>
        {owner && <button type="button" onClick={() => { setRegistration(!registration); setError(''); }}>{registration ? t('Đã có tài khoản? Đăng nhập') : t('Đăng ký chủ quán mới')}</button>}
      </form>}
      {!owner && <p className="muted">Lần chạy đầu tạo tài khoản local trong file <code>ShortVideoTranslator.Api/App_Data/bootstrap-admin.txt</code>. Mật khẩu không được đưa lên Git.</p>}
      {error && <p className="notice" role="alert">{error}</p>}{notice && <p className="success" role="status">{notice}</p>}
    </main> : <div className="portal-layout"><aside className="portal-sidebar"><strong>{user.displayName}</strong><small>{user.role} · phiên 30 phút</small>
      {allowedTabs.map(key => <button key={key} className={tab === key ? 'selected' : ''} onClick={() => selectTab(key)}>{t(labels[key])}</button>)}
      <a href={owner ? '/admin' : '/owner'}>{owner ? t('Cổng quản trị') : t('Cổng chủ quán')}</a>
      <button disabled={busy} onClick={async () => { if (await run(() => mutateApi('/auth/logout'), 'Đã đăng xuất.')) { setUser(null); setData(null); } }}>{t('Đăng xuất')}</button>
    </aside><main className="portal-main">
      <div className="portal-title"><div><span className="eyebrow">{owner ? 'OWNER WORKSPACE' : 'ADMIN WORKSPACE'}</span><h1>{user.mustChangePassword ? t('Đổi mật khẩu ban đầu') : t(labels[tab])}</h1></div><button disabled={busy} onClick={() => run(async () => { setUser(await readApi('/auth/me')); return null; }, 'Đã cập nhật dữ liệu và quyền truy cập.')}>{t('Tải lại')}</button></div>
      {error && <p className="notice" role="alert">{error}</p>}{notice && <p className="success" role="status">{notice}</p>}
      {user.mustChangePassword || tab === 'password' ? <PasswordForm busy={busy} onSubmit={async body => { const outcome = await run(() => mutateApi('/auth/change-password', 'POST', body), 'Đã đổi mật khẩu. Các phiên cũ bị vô hiệu.'); if (outcome) { setUser(outcome.result); setData(null); } }} /> :
        owner && !user.verified && tab !== 'identity' && tab !== 'notifications' ? <RegistrationStatus data={data} busy={busy} run={run} /> : <>
        {!data && !error && !(owner && tab === 'notifications') && <p role="status">{t('Đang tải dữ liệu…')}</p>}
        {tab === 'translations' && data?.places && <TranslationPanel data={data} revision={revision} busy={busy} can={can} run={run} />}
        {tab === 'system' && data?.checks && <section className="portal-card"><h2>Khả năng chạy bản demo</h2><p className={data.coreReady ? 'success' : 'notice'}>{data.coreReady ? 'Kho dữ liệu cốt lõi sẵn sàng.' : 'Kho dữ liệu cần kiểm tra trước khi ghi thay đổi.'} Kiểm tra lúc {new Date(data.checkedAt).toLocaleString('vi-VN')}. Kết quả kho dữ liệu được giữ tối đa 15 giây.</p>
          <Table headings={[t('Thành phần'), t('Trạng thái'), t('Chi tiết')]}>{data.checks.map(check => <tr key={check.name}><td>{check.name}</td><td>{check.state}</td><td>{check.detail}</td></tr>)}</Table>
          <h3>MP3 sẵn có theo ngôn ngữ</h3><Table headings={[t('Ngôn ngữ'), 'Audio đúng nội dung / địa điểm công khai']}>{data.audio.map(item => <tr key={item.language}><td>{item.language}</td><td>{item.ready}/{item.total}</td></tr>)}</Table><p className="muted">AI/thời tiết thiếu cấu hình không chặn đọc địa điểm và nghe MP3 đã tạo. Readiness không chứng minh hosting production hoặc dịch vụ ngoài đang kết nối được.</p>
        </section>}
        {tab === 'overview' && data && (owner ? <Table headings={[t('Địa điểm'), t('Sự kiện được đồng ý')]} >{data.map(item => <tr key={item.placeId}><td>{item.name}</td><td>{item.events}</td></tr>)}</Table> : <><div className="metric-grid">{[[t('Địa điểm'), data.places], [t('Đang công khai'), data.activePlaces], [t('Tài khoản'), data.users], [t('Chờ duyệt'), data.pendingReviews], [t('Sự kiện được đồng ý'), data.events], [t('Thiết bị hoạt động · 5 phút'), data.consentedOnlineDevices], [t('Thống kê đang chờ xử lý'), data.pendingAnalytics]].map(([label, value]) => <div className="metric-card" key={label}><strong>{value}</strong><span>{t(label)}</span></div>)}</div><p className="muted">Thống kê chỉ ghi nhận khi khách đồng ý. Không thu thập vị trí hoặc danh tính cá nhân.</p>{can('analytics:export') && <a href={apiUrl('/admin/analytics/export')}>{t('Tải thống kê CSV')}</a>}</>)}
        {tab === 'places' && Array.isArray(data) && <>
          {(owner || can('poi:create')) && <button className="primary" onClick={() => setEditor({ ...blankPlace })}>{owner ? t('+ Đề xuất địa điểm mới') : t('+ Thêm địa điểm')}</button>}
          {editor && <PlaceEditor key={editor.id} value={editor} busy={busy} owners={owners} owner={owner} onCancel={() => setEditor(null)} onSave={async value => { const outcome = await run(() => mutateApi(owner ? '/owner/submissions' : `/admin/places${value.id ? `/${value.id}` : ''}`, owner || !value.id ? 'POST' : 'PUT', value), owner ? 'Đề xuất đã gửi, chờ Admin duyệt.' : 'Địa điểm đã được lưu.'); if (outcome) setEditor(null); }} run={run} />}
          <Table headings={[t('Địa điểm'), t('Trạng thái'), t('Dữ liệu'), t('Thao tác')]}>{data.map(item => <tr key={item.id}><td><strong>{item.name}</strong><small>{item.category} · {item.region === 'district4' ? 'Lưu trữ Quận 4' : 'Quận 1'}</small></td><td>{item.region === 'district4' ? 'Ngoài phạm vi hiện tại' : item.isActive ? t('Công khai') : item.activationRequested ? 'Chờ audio tiếng Anh' : t('Ẩn')}</td><td>{item.isDemo ? 'Demo' : 'Có nguồn'}<small>{item.locationVerified ? 'Tọa độ đã xác minh' : 'Chưa xác minh tọa độ'}</small></td><td><button onClick={() => setEditor(item)}>{owner ? t('Đề xuất sửa') : t('Sửa')}</button>{!owner && can('poi:delete') && <button className="danger" disabled={busy} onClick={() => { if (window.confirm(`Xóa địa điểm ${item.name} và thực đơn?`)) run(() => mutateApi(`/admin/places/${item.id}`, 'DELETE'), 'Đã xóa địa điểm.'); }}>{t('Xóa')}</button>}</td></tr>)}</Table>
          {data.length === 0 && <p>Chưa có địa điểm. Bạn có thể gửi đề xuất mới.</p>}
        </>}
        {tab === 'users' && Array.isArray(data) && <>
          {can('user:create') && <UserForm roles={roles} busy={busy} onSubmit={body => run(() => mutateApi('/admin/users', 'POST', body), 'Đã tạo tài khoản.')} />}
          <Table headings={[t('Tài khoản'), t('Vai trò'), t('Trạng thái'), t('Thao tác')]}>{data.map(item => <UserRow key={item.id} item={item} roles={roles} actor={user} busy={busy} can={can} run={run} />)}</Table>
        </>}
        {tab === 'identity' && owner && <IdentityForm data={data} busy={busy} run={run} />}
        {tab === 'roles' && data?.roles && <RoleEditor data={data} busy={busy} run={run} can={can} />}
        {(tab === 'reviews' || tab === 'submissions') && Array.isArray(data) && <>{data.length === 0 && <p>{t('Chưa có yêu cầu.')}</p>}{data.map(review => <ReviewCard key={review.id} review={review} owner={owner} busy={busy} run={run} can={can} />)}</>}
        {owner && tab === 'notifications' && <OwnerNotifications key={user.id} actorId={user.id} revision={revision} onChanged={() => setRevision(value => value + 1)} onExpired={() => setUser(null)} onRefreshSession={() => run(async () => { setUser(await readApi('/auth/me')); setData(null); }, 'Đã cập nhật quyền truy cập.')} onRegistration={() => selectTab('places')} />}
        {tab === 'audit' && Array.isArray(data) && <Table headings={[t('Thời gian'), t('Hành động'), t('Tài nguyên'), t('Người thực hiện')]}>{data.map(item => <tr key={item.id}><td>{new Date(item.createdAt).toLocaleString('vi-VN')}</td><td>{item.action}</td><td>{item.resource}</td><td className="mono">{item.actorId}</td></tr>)}</Table>}
        {tab === 'audio' && Array.isArray(data) && <AudioPanel tasks={data} busy={busy} run={run} />}
        {tab === 'runtime' && <section className="portal-card"><p>Chỉ hiển thị phiên khách đã đồng ý riêng. Vị trí làm tròn khoảng 100 m, lưu tối đa 24 giờ; không trộn vào analytics.</p><label>{t('Cửa sổ quan sát')}<select value={runtimeHours} onChange={event => { setRuntimeHours(Number(event.target.value)); setData(null); }}><option value={1}>1 giờ</option><option value={6}>6 giờ</option><option value={24}>24 giờ</option></select></label>
          {data?.positions && <><p>{data.positions.length} bản ghi theo phiên/giờ.</p><Table headings={[t('Phiên ẩn danh'), t('Cập nhật'), t('Vĩ độ'), t('Kinh độ'), t('Sai số GPS')]}>{data.positions.map(item => <tr key={`${item.sessionId}:${item.hour}`}><td>{item.sessionId.slice(0,8)}</td><td>{new Date(item.lastSeen).toLocaleString('vi-VN')}</td><td>{item.latitude.toFixed(3)}</td><td>{item.longitude.toFixed(3)}</td><td>{Math.round(item.accuracy)} m</td></tr>)}</Table>{!data.positions.length && <p>Chưa có vị trí được khách đồng ý chia sẻ trong khoảng này.</p>}</>}
        </section>}

      </>}
    </main></div>}
  </div>;
}

function PasswordForm({ onSubmit, busy }) { const t = usePortalText();
  const [form, setForm] = useState({ currentPassword: '', newPassword: '' });
  return <form className="portal-card compact-form" onSubmit={event => { event.preventDefault(); onSubmit(form); }}><p>{t('Mật khẩu mới cần ít nhất 7 ký tự.')}</p><label>{t('Mật khẩu hiện tại')}<input type="password" required autoComplete="current-password" value={form.currentPassword} onChange={event => setForm({ ...form, currentPassword: event.target.value })} /></label><label>{t('Mật khẩu mới')}<input type="password" required autoComplete="new-password" minLength={7} maxLength={128} value={form.newPassword} onChange={event => setForm({ ...form, newPassword: event.target.value })} /></label><button className="primary" disabled={busy}>{t('Đổi mật khẩu')}</button></form>;
}

function PlaceEditor({ value, onSave, onCancel, busy, owners, owner, run }) { const t = usePortalText();
  const [form, setForm] = useState({ ...blankPlace, ...value, isActive: value.isActive || value.activationRequested || false });
  const set = (key, next) => setForm(previous => ({ ...previous, [key]: next }));
  return <section className="portal-card"><h2>{value.id ? t('Chỉnh sửa địa điểm') : t('Địa điểm mới')}</h2><form onSubmit={event => {
    event.preventDefault(); onSave({ ...form, latitude: form.latitude === '' || form.latitude === null ? null : Number(form.latitude), longitude: form.longitude === '' || form.longitude === null ? null : Number(form.longitude), ownerId: form.ownerId || null, address: form.address || null, imageUrl: form.imageUrl || null, imageNote: form.imageNote || null, sourceUrl: form.sourceUrl || null, triggerRadius: Number(form.triggerRadius), audioPriority: Number(form.audioPriority) });
  }}>
    <div className="form-grid"><label>{t('Tên địa điểm')}<input required maxLength={150} value={form.name} onChange={event => set('name', event.target.value)} /></label><label>{t('Danh mục')}<input required maxLength={80} value={form.category} onChange={event => set('category', event.target.value)} /></label></div>
    <label>{t('Thuyết minh tiếng Việt')}<textarea required rows={6} maxLength={3000} value={form.description} onChange={event => set('description', event.target.value)} /></label><small>{form.description.length}/3000 ký tự</small>
    <AiAdvisorPanel place={form} disabled={busy} onApply={text => set('description', text)} />
    <label>{t('Khu vực')}<select value={form.region} onChange={event => set('region', event.target.value)}><option value="district1">Quận 1 — trang khám phá hiện tại</option><option value="district4">Quận 4 — lưu trữ, không hiển thị trang khám phá</option></select></label>
    <label>{t('Địa chỉ đã kiểm tra')}<input maxLength={300} value={form.address || ''} onChange={event => set('address', event.target.value)} /></label>
    <label>Nguồn thông tin / tọa độ<input type="url" placeholder="https://…" value={form.sourceUrl || ''} onChange={event => set('sourceUrl', event.target.value)} /></label>
    <label>Nguồn xác minh địa chỉ<input type="url" placeholder="https://…" value={form.addressSourceUrl || ''} onChange={event => set('addressSourceUrl', event.target.value)} /></label>
    <label>Nguồn tọa độ (OpenStreetMap hoặc nguồn đã đo)<input type="url" placeholder="https://…" value={form.locationSourceUrl || ''} onChange={event => set('locationSourceUrl', event.target.value)} /></label>
    <div className="form-grid"><label>{t('Vĩ độ')}<input type="number" step="any" min={-90} max={90} value={form.latitude ?? ''} onChange={event => set('latitude', event.target.value)} /></label><label>{t('Kinh độ')}<input type="number" step="any" min={-180} max={180} value={form.longitude ?? ''} onChange={event => set('longitude', event.target.value)} /></label></div>
    <label className="checkbox-label"><input type="checkbox" checked={form.locationVerified} onChange={event => set('locationVerified', event.target.checked)} />Tôi đã kiểm chứng tọa độ từ nguồn trên</label>
    <label>URL ảnh HTTPS hoặc ảnh tải lên<input value={form.imageUrl || ''} onChange={event => set('imageUrl', event.target.value)} /></label>
    {!owner && <label>Tải ảnh PNG/JPEG/WebP (tối đa 5 MB)<input type="file" accept="image/png,image/jpeg,image/webp" disabled={busy} onChange={async event => { const file = event.target.files?.[0]; if (!file) return; const upload = new FormData(); upload.append('file', file); const outcome = await run(() => mutateApi('/media', 'POST', upload), 'Đã tải ảnh. Cần ghi chú nguồn ảnh.'); if (outcome) set('imageUrl', outcome.result.url); }} /></label>}
    <label>Ghi chú nguồn ảnh / ảnh minh họa<input maxLength={500} value={form.imageNote || ''} onChange={event => set('imageNote', event.target.value)} /></label>
    <label>Trang nguồn ảnh<input type="url" value={form.imageSourceUrl || ''} onChange={event => set('imageSourceUrl', event.target.value)} placeholder="https://…" /></label>
    {form.imageUrl && <><label>Trọng tâm ảnh: {form.imageFocusY ?? 25}% từ phía trên<input type="range" min={0} max={100} value={form.imageFocusY ?? 25} onChange={event => set('imageFocusY', Number(event.target.value))} /></label><div className="editor-photo"><img style={{ objectPosition: `center ${form.imageFocusY ?? 25}%` }} src={form.imageUrl.startsWith('/api/') ? baseUrl + form.imageUrl : form.imageUrl} alt="Ảnh đang biên tập" /><strong>{form.name}</strong></div></>}
    <fieldset className="gallery-editor"><legend>Ảnh bổ sung ({form.images?.length || 0}/6)</legend>
      <p className="muted">Dùng ảnh đúng địa điểm. Ghi góc chụp, năm ảnh nếu biết, tác giả và giấy phép; ảnh bìa phía trên được giữ riêng.</p>
      {(form.images || []).map((photo, index) => <fieldset key={index}><legend>Ảnh bổ sung {index + 1}</legend>
        {['url', 'caption', 'credit', 'sourceUrl'].map((field, fieldIndex) => <label key={field}>{['URL ảnh HTTPS hoặc /api/media/', 'Chú thích ảnh', 'Tác giả / giấy phép / ghi chú cắt khung', 'Trang nguồn HTTPS'][fieldIndex]}<input required type={field === 'sourceUrl' ? 'url' : 'text'} maxLength={field === 'caption' ? 200 : field === 'credit' ? 500 : 2000} value={photo[field] || ''} onChange={event => set('images', form.images.map((item, at) => at === index ? { ...item, [field]: event.target.value } : item))} /></label>)}
        <label>Trọng tâm ảnh: {photo.focusY ?? 50}%<input type="range" min={0} max={100} value={photo.focusY ?? 50} onChange={event => set('images', form.images.map((item, at) => at === index ? { ...item, focusY: Number(event.target.value) } : item))} /></label>
        <button type="button" disabled={busy} onClick={() => set('images', form.images.filter((_, at) => at !== index))}>Bỏ ảnh bổ sung {index + 1}</button>
      </fieldset>)}
      <button type="button" disabled={busy || (form.images?.length || 0) >= 6} onClick={() => set('images', [...(form.images || []), { url: '', caption: '', credit: '', sourceUrl: '', focusY: 50 }])}>Thêm ảnh bổ sung</button>
    </fieldset>
    <div className="form-grid"><label>Bán kính kích hoạt (m)<input type="number" min={10} max={1000} value={form.triggerRadius} onChange={event => set('triggerRadius', event.target.value)} /></label><label>Ưu tiên audio (0–100)<input type="number" min={0} max={100} value={form.audioPriority} onChange={event => set('audioPriority', event.target.value)} /></label></div>
    {!owner && <label>Chủ quán<select value={form.ownerId || ''} onChange={event => set('ownerId', event.target.value)}><option value="">Hệ thống quản lý</option>{owners.map(item => <option key={item.id} value={item.id}>{item.displayName} ({item.username})</option>)}</select></label>}
    <div className="form-grid"><label className="checkbox-label"><input type="checkbox" checked={form.isDemo} onChange={event => set('isDemo', event.target.checked)} />{t('Dữ liệu demo')}</label>{!owner && <label className="checkbox-label"><input type="checkbox" checked={form.isActive} onChange={event => set('isActive', event.target.checked)} />Yêu cầu công khai (nội dung chính thức cần audio tiếng Anh)</label>}</div>
    <div className="actions"><button className="primary" disabled={busy}>{owner ? 'Gửi đề xuất để duyệt' : t('Lưu địa điểm')}</button><button type="button" onClick={onCancel}>{t('Đóng')}</button></div>
  </form>{value.id > 0 && <MenuEditor placeId={value.id} owner={owner} run={run} busy={busy} />}</section>;
}

function MenuEditor({ placeId, owner, run, busy }) { const t = usePortalText();
  const [items, setItems] = useState([]);
  const [version, setVersion] = useState(0);
  const [error, setError] = useState('');
  const [form, setForm] = useState({ id: '', name: '', description: '', price: '' });
  const path = `/${owner ? 'owner' : 'admin'}/places/${placeId}/menu`;
  useEffect(() => { const controller = new AbortController(); readApi(path, controller.signal).then(setItems).catch(caught => { if (!controller.signal.aborted) setError(caught.message); }); return () => controller.abort(); }, [path, version]);
  return <section className="menu-editor"><h3>{t('Thực đơn')}</h3>{error && <p className="notice">{error}</p>}<form onSubmit={async event => { event.preventDefault(); const outcome = await run(() => mutateApi(path + (form.id ? `/${form.id}` : ''), form.id ? 'PUT' : 'POST', { ...form, placeId, price: form.price === '' ? null : Number(form.price) })); if (outcome) { setForm({ id: '', name: '', description: '', price: '' }); setVersion(value => value + 1); } }}><div className="form-grid"><label>{t('Tên món')}<input required maxLength={100} value={form.name} onChange={event => setForm({ ...form, name: event.target.value })} /></label><label>{t('Giá tham khảo (VND)')}<input type="number" min={0} step={1000} value={form.price ?? ''} onChange={event => setForm({ ...form, price: event.target.value })} /></label></div><label>{t('Mô tả món')}<input maxLength={500} value={form.description} onChange={event => setForm({ ...form, description: event.target.value })} /></label><button disabled={busy}>{form.id ? t('Lưu món') : t('Thêm món')}</button></form><Table headings={[t('Món ăn'), t('Giá tham khảo'), t('Thao tác')]}>{items.map(item => <tr key={item.id}><td>{item.name}</td><td>{item.price === null ? t('Chưa xác minh') : item.price.toLocaleString('vi-VN') + ' đ'}</td><td><button onClick={() => setForm(item)}>{t('Sửa món')}</button>{!owner && <button onClick={async () => { if (window.confirm('Xóa món ăn này?') && await run(() => mutateApi(path + `/${item.id}`, 'DELETE'))) setVersion(value => value + 1); }}>{t('Xóa món')}</button>}</td></tr>)}</Table></section>;
}

function UserForm({ roles, busy, onSubmit }) { const t = usePortalText();
  const [form, setForm] = useState({ username: '', displayName: '', password: '', role: 'user' });
  return <form className="portal-card form-grid" onSubmit={event => { event.preventDefault(); onSubmit(form); }}><label>{t('Tên đăng nhập')}<input required value={form.username} onChange={event => setForm({ ...form, username: event.target.value })} /></label><label>{t('Tên hiển thị')}<input required value={form.displayName} onChange={event => setForm({ ...form, displayName: event.target.value })} /></label><label>{t('Mật khẩu ban đầu')}<input type="password" required minLength={7} maxLength={128} autoComplete="new-password" value={form.password} onChange={event => setForm({ ...form, password: event.target.value })} /></label><label>{t('Vai trò')}<select value={form.role} onChange={event => setForm({ ...form, role: event.target.value })}>{roles.map(role => <option key={role}>{role}</option>)}</select></label><button className="primary" disabled={busy}>{t('Tạo tài khoản')}</button></form>;
}
function UserRow({ item, roles, actor, busy, can, run }) { const t = usePortalText();
  const [role, setRole] = useState(item.role);
  return <tr><td><strong>{item.displayName}</strong><small>{item.username}</small></td><td><select aria-label={`Vai trò ${item.username}`} value={role} onChange={event => setRole(event.target.value)}><option value={item.role}>{item.role}</option>{roles.filter(value => value !== item.role).map(value => <option key={value}>{value}</option>)}</select></td><td>{item.enabled ? t('Hoạt động') : t('Đã khóa')}<small>{item.role === 'poi_owner' && (item.verified ? 'Chủ quán đã duyệt' : 'Chưa duyệt chủ quán')}</small></td><td>{can('user:update') && <><button disabled={busy} onClick={() => run(() => mutateApi(`/admin/users/${item.id}`, 'PUT', { role, enabled: item.enabled }))}>{t('Lưu vai trò')}</button>{actor.id !== item.id && <button disabled={busy} onClick={() => run(() => mutateApi(`/admin/users/${item.id}`, 'PUT', { role: item.role, enabled: !item.enabled }))}>{item.enabled ? t('Khóa') : t('Mở khóa')}</button>}</>}{can('user:delete') && item.role !== 'super_admin' && actor.id !== item.id && <button className="danger" disabled={busy} onClick={() => { if (window.confirm(`Xóa tài khoản ${item.username}?`)) run(() => mutateApi(`/admin/users/${item.id}`, 'DELETE')); }}>{t('Xóa')}</button>}</td></tr>;
}
function RoleEditor({ data, busy, run, can }) { const t = usePortalText();
  const [form, setForm] = useState({ name: '', permissions: [] });
  const editing = data.roles.some(role => role.name === form.name);
  return <><section className="portal-card"><h2>{t('Vai trò tùy chỉnh')}</h2><form onSubmit={event => { event.preventDefault(); run(() => mutateApi(editing ? `/admin/roles/${form.name}` : '/admin/roles', editing ? 'PUT' : 'POST', form)); }}><label>{t('Tên vai trò')}<input required pattern="[a-z_]{3,40}" placeholder="content_editor" value={form.name} onChange={event => setForm({ ...form, name: event.target.value })} /></label><div className="permission-grid">{data.permissions.map(permission => <label className="checkbox-label" key={permission}><input type="checkbox" checked={form.permissions.includes(permission)} onChange={event => setForm({ ...form, permissions: event.target.checked ? [...form.permissions, permission] : form.permissions.filter(item => item !== permission) })} />{permission}</label>)}</div><button className="primary" disabled={busy || !can(editing ? 'role:update' : 'role:create')}>{t('Lưu vai trò tùy chỉnh')}</button></form></section><Table headings={[t('Vai trò'), t('Quyền'), t('Thao tác')]}>{data.roles.map(role => <tr key={role.name}><td>{role.name}<small>{role.builtIn ? t('Vai trò hệ thống') : t('Tùy chỉnh')}</small></td><td>{role.permissions.join(', ')}</td><td>{!role.builtIn && <><button onClick={() => setForm(role)}>{t('Sửa')}</button><button disabled={busy || !can('role:delete')} onClick={() => { if (window.confirm(`Xóa vai trò ${role.name}?`)) run(() => mutateApi(`/admin/roles/${role.name}`, 'DELETE')); }}>{t('Xóa')}</button></>}</td></tr>)}</Table></>;
}
function ReviewCard({ review, owner, busy, run, can }) { const t = usePortalText();
  const [note, setNote] = useState('');
  const [identity, setIdentity] = useState(null);
  return <section className="portal-card"><div className="portal-title"><h2>{review.kind === 'registration' ? t('Đăng ký chủ quán') : review.place?.name}</h2><Badge status={review.status} /></div><small>Người gửi: {review.userId} · {new Date(review.createdAt).toLocaleString('vi-VN')}</small>{review.applicantNote && <div className="notice"><strong>{t('Ghi chú gửi Admin')}</strong><p className="notification-message">{review.applicantNote}</p></div>}{review.place && <><p>{review.place.description}</p><p>Nguồn: {review.place.sourceUrl || 'Chưa có — dữ liệu demo'}</p><p>Địa chỉ: {review.place.address || t('Chưa xác minh')}</p></>}{review.adminNote && <p className="notice">Ghi chú Admin: {review.adminNote}</p>}{!owner && review.kind === 'registration' && can('user:read') && can('poi:approve') && <div>{identity ? <><p>{identity.present ? `Thông tin xác minh: ${identity.number}` : 'Chưa có giấy tờ, đã hết hạn hoặc không giải mã được.'}</p><button onClick={() => setIdentity(null)}>Ẩn thông tin xác minh</button></> : <button disabled={busy} onClick={async () => { const outcome = await run(() => readApi(`/admin/users/${review.userId}/identity`), 'Đã kiểm tra thông tin xác minh; thao tác đã ghi nhật ký.'); if (outcome) setIdentity(outcome.result); }}>Xem thông tin xác minh</button>}</div>}{!owner && review.status === 'pending' && <><label>{t('Ghi chú duyệt')}<textarea maxLength={1000} value={note} onChange={event => setNote(event.target.value)} /></label><div className="actions"><button className="primary" disabled={busy} onClick={() => run(() => mutateApi(`/admin/reviews/${review.id}/decision`, 'POST', { approve: true, adminNote: note }), 'Đã duyệt và thông báo chủ quán.')}>{t('Phê duyệt')}</button><button className="danger" disabled={busy || !note.trim()} onClick={() => run(() => mutateApi(`/admin/reviews/${review.id}/decision`, 'POST', { approve: false, adminNote: note }), 'Đã từ chối và gửi lý do.')}>{t('Từ chối')}</button></div></>}</section>;
}
function AudioPanel({ tasks, busy, run }) { const t = usePortalText();
  const [places, setPlaces] = useState([]);
  const [placeId, setPlaceId] = useState('');
  const [languages, setLanguages] = useState(['vi']);
  const [error, setError] = useState('');
  useEffect(() => { const controller = new AbortController(); readApi('/admin/places', controller.signal).then(items => { setPlaces(items); setPlaceId(String(items[0]?.id || '')); }).catch(caught => { if (!controller.signal.aborted) setError(caught.message); }); return () => controller.abort(); }, []);
  return <><form className="portal-card" onSubmit={event => { event.preventDefault(); run(() => mutateApi('/admin/audio-tasks', 'POST', { placeId: Number(placeId), languages }), 'Đã xếp hàng tạo audio.'); }}><label>{t('Địa điểm')}<select required value={placeId} onChange={event => setPlaceId(event.target.value)}>{places.map(place => <option key={place.id} value={place.id}>{place.name}</option>)}</select></label><div className="actions">{['vi', 'en', 'zh', 'ja', 'ko', 'fr'].map(language => <label className="checkbox-label" key={language}><input type="checkbox" checked={languages.includes(language)} onChange={event => setLanguages(event.target.checked ? [...languages, language] : languages.filter(item => item !== language))} />{language}</label>)}</div><button className="primary" disabled={busy || !languages.length || !placeId}>{t('Tạo MP3 theo ngôn ngữ')}</button>{error && <p className="notice">{error}</p>}</form><Table headings={[t('Địa điểm'), t('Trạng thái'), t('Tiến độ'), t('Điều khiển')]}>{tasks.map(task => <tr key={task.id}><td>{places.find(place => place.id === task.placeId)?.name || task.placeId}<small>{task.languages.join(', ')}</small><small>{new Date(task.createdAt).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })}</small></td><td><Badge status={task.status} />{task.error && <small className="danger">{task.error}</small>}</td><td><progress value={task.completed} max={task.languages.length} /> {task.completed}/{task.languages.length}</td><td>{['queued', 'running'].includes(task.status) && <button disabled={busy} onClick={() => run(() => mutateApi(`/admin/audio-tasks/${task.id}/pause`))}>{t('Tạm dừng')}</button>}{task.status === 'paused' && <button disabled={busy} onClick={() => run(() => mutateApi(`/admin/audio-tasks/${task.id}/resume`))}>{t('Tiếp tục')}</button>}{['queued', 'running', 'paused'].includes(task.status) && <button className="danger" disabled={busy} onClick={() => run(() => mutateApi(`/admin/audio-tasks/${task.id}/cancel`))}>{t('Hủy')}</button>}</td></tr>)}</Table></>;
}

function IdentityForm({ data, busy, run }) { const t = usePortalText();
  const [number, setNumber] = useState('');
  return <section className="portal-card compact-form"><h2>{t('Thông tin xác minh tùy chọn')}</h2><p>Không bắt buộc cho bản demo. Số CCCD được gửi riêng tới backend SmartTour, lưu mã hóa và chỉ Admin có quyền kiểm duyệt được xem. Hết quyền đọc sau 180 ngày, dọn định kỳ mỗi giờ. Không nhập giấy tờ thật khi demo trên máy dùng chung.</p>
    <p>{data?.present ? `Đã lưu: ${data.masked} · hết hạn ${new Date(data.expiresAt).toLocaleDateString('vi-VN')}` : 'Chưa có thông tin xác minh đang hiệu lực.'}</p>
    <form onSubmit={async event => { event.preventDefault(); const outcome = await run(() => mutateApi('/owner/identity', 'PUT', { number }), 'Đã lưu thông tin xác minh được mã hóa.'); if (outcome) setNumber(''); }}><label>Số CCCD · tùy chọn<input type="password" inputMode="numeric" autoComplete="off" pattern="[0-9]{12}" minLength={12} maxLength={12} required value={number} onChange={event => setNumber(event.target.value)} /></label><button className="primary" disabled={busy}>{t('Lưu thông tin xác minh')}</button></form>
    {data?.present && <button disabled={busy} onClick={() => run(() => mutateApi('/owner/identity', 'DELETE'), 'Đã xóa thông tin xác minh.')}>{t('Xóa thông tin xác minh')}</button>}
  </section>;
}
