import { useEffect, useState } from 'react';
import { apiUrl, baseUrl, mutateApi, readApi } from './api';
import './App.css';
import './Portal.css';

const labels = { overview: 'Tổng quan', places: 'Địa điểm', users: 'Tài khoản', roles: 'Vai trò', reviews: 'Kiểm duyệt', audio: 'Tác vụ audio', audit: 'Nhật ký', submissions: 'Đề xuất', notifications: 'Thông báo', runtime: 'Giám sát vị trí', identity: 'Xác minh chủ quán', password: 'Mật khẩu' };
const permissions = { overview: 'analytics:view', places: 'poi:update', users: 'user:read', roles: 'role:read', reviews: 'content:moderate', audio: 'poi:update', audit: 'audit:read', runtime: 'system:logs' };
const statuses = { pending: 'Chờ duyệt', approved: 'Đã duyệt', rejected: 'Từ chối', queued: 'Đang chờ', running: 'Đang tạo', paused: 'Tạm dừng', completed: 'Hoàn tất', failed: 'Thất bại', cancelled: 'Đã hủy' };
const blankPlace = { region: 'district1', id: 0, name: '', category: 'Ẩm thực', description: '', address: '', latitude: '', longitude: '', sourceUrl: '', isDemo: true, isActive: false, locationVerified: false, imageUrl: '', imageNote: '', ownerId: '', triggerRadius: 30, audioPriority: 0, updatedAt: null };

function Table({ headings, children }) { return <div className="table-scroll"><table><thead><tr>{headings.map(heading => <th key={heading}>{heading}</th>)}</tr></thead><tbody>{children}</tbody></table></div>; }
function Badge({ status }) { return <span className={`badge badge-${status}`}>{statuses[status] || status}</span>; }

export default function Portal() {
  const owner = window.location.pathname.startsWith('/owner');
  const [user, setUser] = useState(null);
  const [initializing, setInitializing] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(false);
  const [selectedTab, setTab] = useState(owner ? 'places' : 'overview');
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
    const controller = new AbortController();
    readApi('/auth/me', controller.signal).then(setUser).catch(caught => {
      if (!controller.signal.aborted && caught.status !== 401) setError(caught.message);
    }).finally(() => { if (!controller.signal.aborted) setInitializing(false); });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    if (!user || user.mustChangePassword || tab === 'password') return;
    const controller = new AbortController();
    const endpoints = owner ? { places: '/owner/places', overview: '/owner/analytics', submissions: '/owner/submissions', notifications: '/owner/notifications', identity: '/owner/identity' }
      : { overview: '/admin/analytics', places: '/admin/places', users: '/admin/users', roles: '/admin/roles', reviews: '/admin/reviews', audio: '/admin/audio-tasks', audit: '/admin/audit', runtime: `/runtime/window?hours=${runtimeHours}` };
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
  function selectTab(value) { setTab(value); setData(null); setEditor(null); setError(''); setNotice(''); }
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
    <header className="portal-header"><a href="/" className="portal-brand">📍 SmartTour Guide</a><span>{owner ? 'Cổng chủ quán' : 'Quản trị hệ thống'}</span><a href="/">← Trang khám phá</a></header>
    {!user ? <main className="login-card"><span className="eyebrow">SMARTTOUR PORTAL</span><h1>{registration ? 'Đăng ký chủ quán' : 'Đăng nhập'}</h1>
      <p>{owner ? 'Quản lý thông tin quán và gửi nội dung để Admin duyệt.' : 'Dành cho tài khoản có quyền quản trị.'}</p>
      {initializing ? <p role="status">Đang kiểm tra phiên đăng nhập…</p> : <form onSubmit={login}>
        <label>Tên đăng nhập<input required autoComplete="username" value={credentials.username} onChange={event => setCredentials({ ...credentials, username: event.target.value })} maxLength={80} /></label>
        {registration && <label>Tên hiển thị<input required value={credentials.displayName} onChange={event => setCredentials({ ...credentials, displayName: event.target.value })} maxLength={100} /></label>}
        <label>Mật khẩu<input type="password" required autoComplete={registration ? 'new-password' : 'current-password'} minLength={registration ? 7 : 1} maxLength={128} value={credentials.password} onChange={event => setCredentials({ ...credentials, password: event.target.value })} /></label>
        <button className="primary" disabled={busy}>{busy ? 'Đang xử lý…' : registration ? 'Gửi đăng ký' : 'Đăng nhập'}</button>
        {owner && <button type="button" onClick={() => { setRegistration(!registration); setError(''); }}>{registration ? 'Đã có tài khoản? Đăng nhập' : 'Đăng ký chủ quán mới'}</button>}
      </form>}
      {!owner && <p className="muted">Lần chạy đầu tạo tài khoản local trong file <code>ShortVideoTranslator.Api/App_Data/bootstrap-admin.txt</code>. Mật khẩu không được đưa lên Git.</p>}
      {error && <p className="notice" role="alert">{error}</p>}{notice && <p className="success" role="status">{notice}</p>}
    </main> : <div className="portal-layout"><aside className="portal-sidebar"><strong>{user.displayName}</strong><small>{user.role} · phiên 30 phút</small>
      {allowedTabs.map(key => <button key={key} className={tab === key ? 'selected' : ''} onClick={() => selectTab(key)}>{labels[key]}</button>)}
      <a href={owner ? '/admin' : '/owner'}>{owner ? 'Cổng quản trị' : 'Cổng chủ quán'}</a>
      <button disabled={busy} onClick={async () => { if (await run(() => mutateApi('/auth/logout'), 'Đã đăng xuất.')) { setUser(null); setData(null); } }}>Đăng xuất</button>
    </aside><main className="portal-main">
      <div className="portal-title"><div><span className="eyebrow">{owner ? 'OWNER WORKSPACE' : 'ADMIN WORKSPACE'}</span><h1>{user.mustChangePassword ? 'Đổi mật khẩu ban đầu' : labels[tab]}</h1></div><button disabled={busy} onClick={() => run(async () => { setUser(await readApi('/auth/me')); return null; }, 'Đã cập nhật dữ liệu và quyền truy cập.')}>Tải lại</button></div>
      {error && <p className="notice" role="alert">{error}</p>}{notice && <p className="success" role="status">{notice}</p>}
      {user.mustChangePassword || tab === 'password' ? <PasswordForm busy={busy} onSubmit={async body => { const outcome = await run(() => mutateApi('/auth/change-password', 'POST', body), 'Đã đổi mật khẩu. Các phiên cũ bị vô hiệu.'); if (outcome) { setUser(outcome.result); setData(null); } }} /> :
        owner && !user.verified && tab !== 'identity' ? <section className="portal-card"><h2>Tài khoản đang chờ duyệt</h2><p>Admin cần xác minh chủ quán trước khi bạn quản lý địa điểm.</p>{data?.reviews?.map(review => <p key={review.id}><Badge status={review.status} /> {review.adminNote}</p>)}</section> : <>
        {!data && !error && <p role="status">Đang tải dữ liệu…</p>}
        {tab === 'overview' && data && (owner ? <Table headings={['Địa điểm', 'Sự kiện được đồng ý']} >{data.map(item => <tr key={item.placeId}><td>{item.name}</td><td>{item.events}</td></tr>)}</Table> : <><div className="metric-grid">{[['Địa điểm', data.places], ['Đang công khai', data.activePlaces], ['Tài khoản', data.users], ['Chờ duyệt', data.pendingReviews], ['Sự kiện được đồng ý', data.events], ['Thiết bị hoạt động · 5 phút', data.consentedOnlineDevices]].map(([label, value]) => <div className="metric-card" key={label}><strong>{value}</strong><span>{label}</span></div>)}</div><p className="muted">Thống kê chỉ ghi nhận khi khách đồng ý. Không thu thập vị trí hoặc danh tính cá nhân.</p>{can('analytics:export') && <a href={apiUrl('/admin/analytics/export')}>Tải thống kê CSV</a>}</>)}
        {tab === 'places' && Array.isArray(data) && <>
          {(owner || can('poi:create')) && <button className="primary" onClick={() => setEditor({ ...blankPlace })}>{owner ? '+ Đề xuất địa điểm mới' : '+ Thêm địa điểm'}</button>}
          {editor && <PlaceEditor key={editor.id} value={editor} busy={busy} owners={owners} owner={owner} onCancel={() => setEditor(null)} onSave={async value => { const outcome = await run(() => mutateApi(owner ? '/owner/submissions' : `/admin/places${value.id ? `/${value.id}` : ''}`, owner || !value.id ? 'POST' : 'PUT', value), owner ? 'Đề xuất đã gửi, chờ Admin duyệt.' : 'Địa điểm đã được lưu.'); if (outcome) setEditor(null); }} run={run} />}
          <Table headings={['Địa điểm', 'Trạng thái', 'Dữ liệu', 'Thao tác']}>{data.map(item => <tr key={item.id}><td><strong>{item.name}</strong><small>{item.category} · {item.region === 'district4' ? 'Lưu trữ Quận 4' : 'Quận 1'}</small></td><td>{item.region === 'district4' ? 'Ngoài phạm vi hiện tại' : item.isActive ? 'Công khai' : item.activationRequested ? 'Chờ audio tiếng Anh' : 'Ẩn'}</td><td>{item.isDemo ? 'Demo' : 'Có nguồn'}<small>{item.locationVerified ? 'Tọa độ đã xác minh' : 'Chưa xác minh tọa độ'}</small></td><td><button onClick={() => setEditor(item)}>{owner ? 'Đề xuất sửa' : 'Sửa'}</button>{!owner && can('poi:delete') && <button className="danger" disabled={busy} onClick={() => { if (window.confirm(`Xóa địa điểm ${item.name} và thực đơn?`)) run(() => mutateApi(`/admin/places/${item.id}`, 'DELETE'), 'Đã xóa địa điểm.'); }}>Xóa</button>}</td></tr>)}</Table>
          {data.length === 0 && <p>Chưa có địa điểm. Bạn có thể gửi đề xuất mới.</p>}
        </>}
        {tab === 'users' && Array.isArray(data) && <>
          {can('user:create') && <UserForm roles={roles} busy={busy} onSubmit={body => run(() => mutateApi('/admin/users', 'POST', body), 'Đã tạo tài khoản.')} />}
          <Table headings={['Tài khoản', 'Vai trò', 'Trạng thái', 'Thao tác']}>{data.map(item => <UserRow key={item.id} item={item} roles={roles} actor={user} busy={busy} can={can} run={run} />)}</Table>
        </>}
        {tab === 'identity' && owner && <IdentityForm data={data} busy={busy} run={run} />}
        {tab === 'roles' && data?.roles && <RoleEditor data={data} busy={busy} run={run} can={can} />}
        {(tab === 'reviews' || tab === 'submissions') && Array.isArray(data) && <>{data.length === 0 && <p>Chưa có yêu cầu.</p>}{data.map(review => <ReviewCard key={review.id} review={review} owner={owner} busy={busy} run={run} can={can} />)}</>}
        {tab === 'notifications' && Array.isArray(data) && <>{!data.length && <p>Chưa có thông báo.</p>}{data.map(item => <section className="portal-card" key={item.id}><p>{item.message}</p><small>{new Date(item.createdAt).toLocaleString('vi-VN')}</small>{!item.read && <button onClick={() => run(() => mutateApi(`/owner/notifications/${item.id}/read`), 'Đã đọc thông báo.')}>Đánh dấu đã đọc</button>}</section>)}</>}
        {tab === 'audit' && Array.isArray(data) && <Table headings={['Thời gian', 'Hành động', 'Tài nguyên', 'Người thực hiện']}>{data.map(item => <tr key={item.id}><td>{new Date(item.createdAt).toLocaleString('vi-VN')}</td><td>{item.action}</td><td>{item.resource}</td><td className="mono">{item.actorId}</td></tr>)}</Table>}
        {tab === 'audio' && Array.isArray(data) && <AudioPanel tasks={data} busy={busy} run={run} />}
        {tab === 'runtime' && <section className="portal-card"><p>Chỉ hiển thị phiên khách đã đồng ý riêng. Vị trí làm tròn khoảng 100 m, lưu tối đa 24 giờ; không trộn vào analytics.</p><label>Cửa sổ quan sát<select value={runtimeHours} onChange={event => { setRuntimeHours(Number(event.target.value)); setData(null); }}><option value={1}>1 giờ</option><option value={6}>6 giờ</option><option value={24}>24 giờ</option></select></label>
          {data?.positions && <><p>{data.positions.length} bản ghi theo phiên/giờ.</p><Table headings={['Phiên ẩn danh', 'Cập nhật', 'Vĩ độ', 'Kinh độ', 'Sai số GPS']}>{data.positions.map(item => <tr key={`${item.sessionId}:${item.hour}`}><td>{item.sessionId.slice(0,8)}</td><td>{new Date(item.lastSeen).toLocaleString('vi-VN')}</td><td>{item.latitude.toFixed(3)}</td><td>{item.longitude.toFixed(3)}</td><td>{Math.round(item.accuracy)} m</td></tr>)}</Table>{!data.positions.length && <p>Chưa có vị trí được khách đồng ý chia sẻ trong khoảng này.</p>}</>}
        </section>}

      </>}
    </main></div>}
  </div>;
}

function PasswordForm({ onSubmit, busy }) {
  const [form, setForm] = useState({ currentPassword: '', newPassword: '' });
  return <form className="portal-card compact-form" onSubmit={event => { event.preventDefault(); onSubmit(form); }}><p>Mật khẩu mới cần ít nhất 7 ký tự.</p><label>Mật khẩu hiện tại<input type="password" required autoComplete="current-password" value={form.currentPassword} onChange={event => setForm({ ...form, currentPassword: event.target.value })} /></label><label>Mật khẩu mới<input type="password" required autoComplete="new-password" minLength={7} maxLength={128} value={form.newPassword} onChange={event => setForm({ ...form, newPassword: event.target.value })} /></label><button className="primary" disabled={busy}>Đổi mật khẩu</button></form>;
}

function PlaceEditor({ value, onSave, onCancel, busy, owners, owner, run }) {
  const [form, setForm] = useState({ ...blankPlace, ...value, isActive: value.isActive || value.activationRequested || false });
  const set = (key, next) => setForm(previous => ({ ...previous, [key]: next }));
  const [aiDraft, setAiDraft] = useState('');
  return <section className="portal-card"><h2>{value.id ? 'Chỉnh sửa địa điểm' : 'Địa điểm mới'}</h2><form onSubmit={event => {
    event.preventDefault(); onSave({ ...form, latitude: form.latitude === '' || form.latitude === null ? null : Number(form.latitude), longitude: form.longitude === '' || form.longitude === null ? null : Number(form.longitude), ownerId: form.ownerId || null, address: form.address || null, imageUrl: form.imageUrl || null, imageNote: form.imageNote || null, sourceUrl: form.sourceUrl || null, triggerRadius: Number(form.triggerRadius), audioPriority: Number(form.audioPriority) });
  }}>
    <div className="form-grid"><label>Tên địa điểm<input required maxLength={150} value={form.name} onChange={event => set('name', event.target.value)} /></label><label>Danh mục<input required maxLength={80} value={form.category} onChange={event => set('category', event.target.value)} /></label></div>
    <label>Thuyết minh tiếng Việt<textarea required rows={6} maxLength={3000} value={form.description} onChange={event => set('description', event.target.value)} /></label><small>{form.description.length}/3000 ký tự</small>
    <button type="button" disabled={busy} onClick={async () => { const outcome = await run(() => mutateApi('/ai/enhance-description', 'POST', { text: form.description }), 'AI đã tạo bản nháp; hãy kiểm chứng trước khi sử dụng.'); if (outcome) setAiDraft(outcome.result.text); }}>AI hỗ trợ biên tập</button>
    {aiDraft && <div className="notice"><p>{aiDraft}</p><button type="button" onClick={() => { set('description', aiDraft); setAiDraft(''); }}>Dùng bản nháp đã rà soát</button></div>}
    <label>Khu vực<select value={form.region} onChange={event => set('region', event.target.value)}><option value="district1">Quận 1 — trang khám phá hiện tại</option><option value="district4">Quận 4 — lưu trữ, không hiển thị trang khám phá</option></select></label>
    <label>Địa chỉ đã kiểm tra<input maxLength={300} value={form.address || ''} onChange={event => set('address', event.target.value)} /></label>
    <label>Nguồn thông tin / tọa độ<input type="url" placeholder="https://…" value={form.sourceUrl || ''} onChange={event => set('sourceUrl', event.target.value)} /></label>
    <label>Nguồn xác minh địa chỉ<input type="url" placeholder="https://…" value={form.addressSourceUrl || ''} onChange={event => set('addressSourceUrl', event.target.value)} /></label>
    <label>Nguồn tọa độ (OpenStreetMap hoặc nguồn đã đo)<input type="url" placeholder="https://…" value={form.locationSourceUrl || ''} onChange={event => set('locationSourceUrl', event.target.value)} /></label>
    <div className="form-grid"><label>Vĩ độ<input type="number" step="any" min={-90} max={90} value={form.latitude ?? ''} onChange={event => set('latitude', event.target.value)} /></label><label>Kinh độ<input type="number" step="any" min={-180} max={180} value={form.longitude ?? ''} onChange={event => set('longitude', event.target.value)} /></label></div>
    <label className="checkbox-label"><input type="checkbox" checked={form.locationVerified} onChange={event => set('locationVerified', event.target.checked)} />Tôi đã kiểm chứng tọa độ từ nguồn trên</label>
    <label>URL ảnh HTTPS hoặc ảnh tải lên<input value={form.imageUrl || ''} onChange={event => set('imageUrl', event.target.value)} /></label>
    {!owner && <label>Tải ảnh PNG/JPEG/WebP (tối đa 5 MB)<input type="file" accept="image/png,image/jpeg,image/webp" disabled={busy} onChange={async event => { const file = event.target.files?.[0]; if (!file) return; const upload = new FormData(); upload.append('file', file); const outcome = await run(() => mutateApi('/media', 'POST', upload), 'Đã tải ảnh. Cần ghi chú nguồn ảnh.'); if (outcome) set('imageUrl', outcome.result.url); }} /></label>}
    <label>Ghi chú nguồn ảnh / ảnh minh họa<input maxLength={500} value={form.imageNote || ''} onChange={event => set('imageNote', event.target.value)} /></label>
    <label>Trang nguồn ảnh<input type="url" value={form.imageSourceUrl || ''} onChange={event => set('imageSourceUrl', event.target.value)} placeholder="https://…" /></label>
    {form.imageUrl && <><label>Trọng tâm ảnh: {form.imageFocusY ?? 25}% từ phía trên<input type="range" min={0} max={100} value={form.imageFocusY ?? 25} onChange={event => set('imageFocusY', Number(event.target.value))} /></label><div className="editor-photo"><img style={{ objectPosition: `center ${form.imageFocusY ?? 25}%` }} src={form.imageUrl.startsWith('/api/') ? baseUrl + form.imageUrl : form.imageUrl} alt="Ảnh đang biên tập" /><strong>{form.name}</strong></div></>}
    <div className="form-grid"><label>Bán kính kích hoạt (m)<input type="number" min={10} max={1000} value={form.triggerRadius} onChange={event => set('triggerRadius', event.target.value)} /></label><label>Ưu tiên audio (0–100)<input type="number" min={0} max={100} value={form.audioPriority} onChange={event => set('audioPriority', event.target.value)} /></label></div>
    {!owner && <label>Chủ quán<select value={form.ownerId || ''} onChange={event => set('ownerId', event.target.value)}><option value="">Hệ thống quản lý</option>{owners.map(item => <option key={item.id} value={item.id}>{item.displayName} ({item.username})</option>)}</select></label>}
    <div className="form-grid"><label className="checkbox-label"><input type="checkbox" checked={form.isDemo} onChange={event => set('isDemo', event.target.checked)} />Dữ liệu demo</label>{!owner && <label className="checkbox-label"><input type="checkbox" checked={form.isActive} onChange={event => set('isActive', event.target.checked)} />Yêu cầu công khai (nội dung chính thức cần audio tiếng Anh)</label>}</div>
    <div className="actions"><button className="primary" disabled={busy}>{owner ? 'Gửi đề xuất để duyệt' : 'Lưu địa điểm'}</button><button type="button" onClick={onCancel}>Đóng</button></div>
  </form>{value.id > 0 && <MenuEditor placeId={value.id} owner={owner} run={run} busy={busy} />}</section>;
}

function MenuEditor({ placeId, owner, run, busy }) {
  const [items, setItems] = useState([]);
  const [version, setVersion] = useState(0);
  const [error, setError] = useState('');
  const [form, setForm] = useState({ id: '', name: '', description: '', price: '' });
  const path = `/${owner ? 'owner' : 'admin'}/places/${placeId}/menu`;
  useEffect(() => { const controller = new AbortController(); readApi(path, controller.signal).then(setItems).catch(caught => { if (!controller.signal.aborted) setError(caught.message); }); return () => controller.abort(); }, [path, version]);
  return <section className="menu-editor"><h3>Thực đơn</h3>{error && <p className="notice">{error}</p>}<form onSubmit={async event => { event.preventDefault(); const outcome = await run(() => mutateApi(path + (form.id ? `/${form.id}` : ''), form.id ? 'PUT' : 'POST', { ...form, placeId, price: form.price === '' ? null : Number(form.price) })); if (outcome) { setForm({ id: '', name: '', description: '', price: '' }); setVersion(value => value + 1); } }}><div className="form-grid"><label>Tên món<input required maxLength={100} value={form.name} onChange={event => setForm({ ...form, name: event.target.value })} /></label><label>Giá tham khảo (VND)<input type="number" min={0} step={1000} value={form.price ?? ''} onChange={event => setForm({ ...form, price: event.target.value })} /></label></div><label>Mô tả món<input maxLength={500} value={form.description} onChange={event => setForm({ ...form, description: event.target.value })} /></label><button disabled={busy}>{form.id ? 'Lưu món' : 'Thêm món'}</button></form><Table headings={['Món ăn', 'Giá tham khảo', 'Thao tác']}>{items.map(item => <tr key={item.id}><td>{item.name}</td><td>{item.price === null ? 'Chưa xác minh' : item.price.toLocaleString('vi-VN') + ' đ'}</td><td><button onClick={() => setForm(item)}>Sửa món</button>{!owner && <button onClick={async () => { if (window.confirm('Xóa món ăn này?') && await run(() => mutateApi(path + `/${item.id}`, 'DELETE'))) setVersion(value => value + 1); }}>Xóa món</button>}</td></tr>)}</Table></section>;
}

function UserForm({ roles, busy, onSubmit }) {
  const [form, setForm] = useState({ username: '', displayName: '', password: '', role: 'user' });
  return <form className="portal-card form-grid" onSubmit={event => { event.preventDefault(); onSubmit(form); }}><label>Tên đăng nhập<input required value={form.username} onChange={event => setForm({ ...form, username: event.target.value })} /></label><label>Tên hiển thị<input required value={form.displayName} onChange={event => setForm({ ...form, displayName: event.target.value })} /></label><label>Mật khẩu ban đầu<input type="password" required minLength={7} maxLength={128} autoComplete="new-password" value={form.password} onChange={event => setForm({ ...form, password: event.target.value })} /></label><label>Vai trò<select value={form.role} onChange={event => setForm({ ...form, role: event.target.value })}>{roles.map(role => <option key={role}>{role}</option>)}</select></label><button className="primary" disabled={busy}>Tạo tài khoản</button></form>;
}
function UserRow({ item, roles, actor, busy, can, run }) {
  const [role, setRole] = useState(item.role);
  return <tr><td><strong>{item.displayName}</strong><small>{item.username}</small></td><td><select aria-label={`Vai trò ${item.username}`} value={role} onChange={event => setRole(event.target.value)}><option value={item.role}>{item.role}</option>{roles.filter(value => value !== item.role).map(value => <option key={value}>{value}</option>)}</select></td><td>{item.enabled ? 'Hoạt động' : 'Đã khóa'}<small>{item.role === 'poi_owner' && (item.verified ? 'Chủ quán đã duyệt' : 'Chưa duyệt chủ quán')}</small></td><td>{can('user:update') && <><button disabled={busy} onClick={() => run(() => mutateApi(`/admin/users/${item.id}`, 'PUT', { role, enabled: item.enabled }))}>Lưu vai trò</button>{actor.id !== item.id && <button disabled={busy} onClick={() => run(() => mutateApi(`/admin/users/${item.id}`, 'PUT', { role: item.role, enabled: !item.enabled }))}>{item.enabled ? 'Khóa' : 'Mở khóa'}</button>}</>}{can('user:delete') && item.role !== 'super_admin' && actor.id !== item.id && <button className="danger" disabled={busy} onClick={() => { if (window.confirm(`Xóa tài khoản ${item.username}?`)) run(() => mutateApi(`/admin/users/${item.id}`, 'DELETE')); }}>Xóa</button>}</td></tr>;
}
function RoleEditor({ data, busy, run, can }) {
  const [form, setForm] = useState({ name: '', permissions: [] });
  const editing = data.roles.some(role => role.name === form.name);
  return <><section className="portal-card"><h2>Vai trò tùy chỉnh</h2><form onSubmit={event => { event.preventDefault(); run(() => mutateApi(editing ? `/admin/roles/${form.name}` : '/admin/roles', editing ? 'PUT' : 'POST', form)); }}><label>Tên vai trò<input required pattern="[a-z_]{3,40}" placeholder="content_editor" value={form.name} onChange={event => setForm({ ...form, name: event.target.value })} /></label><div className="permission-grid">{data.permissions.map(permission => <label className="checkbox-label" key={permission}><input type="checkbox" checked={form.permissions.includes(permission)} onChange={event => setForm({ ...form, permissions: event.target.checked ? [...form.permissions, permission] : form.permissions.filter(item => item !== permission) })} />{permission}</label>)}</div><button className="primary" disabled={busy || !can(editing ? 'role:update' : 'role:create')}>Lưu vai trò tùy chỉnh</button></form></section><Table headings={['Vai trò', 'Quyền', 'Thao tác']}>{data.roles.map(role => <tr key={role.name}><td>{role.name}<small>{role.builtIn ? 'Vai trò hệ thống' : 'Tùy chỉnh'}</small></td><td>{role.permissions.join(', ')}</td><td>{!role.builtIn && <><button onClick={() => setForm(role)}>Sửa</button><button disabled={busy || !can('role:delete')} onClick={() => { if (window.confirm(`Xóa vai trò ${role.name}?`)) run(() => mutateApi(`/admin/roles/${role.name}`, 'DELETE')); }}>Xóa</button></>}</td></tr>)}</Table></>;
}
function ReviewCard({ review, owner, busy, run, can }) {
  const [note, setNote] = useState('');
  const [identity, setIdentity] = useState(null);
  return <section className="portal-card"><div className="portal-title"><h2>{review.kind === 'registration' ? 'Đăng ký chủ quán' : review.place?.name}</h2><Badge status={review.status} /></div><small>Người gửi: {review.userId} · {new Date(review.createdAt).toLocaleString('vi-VN')}</small>{review.place && <><p>{review.place.description}</p><p>Nguồn: {review.place.sourceUrl || 'Chưa có — dữ liệu demo'}</p><p>Địa chỉ: {review.place.address || 'Chưa xác minh'}</p></>}{review.adminNote && <p className="notice">Ghi chú Admin: {review.adminNote}</p>}{!owner && review.kind === 'registration' && can('user:read') && can('poi:approve') && <div>{identity ? <><p>{identity.present ? `Thông tin xác minh: ${identity.number}` : 'Chưa có giấy tờ, đã hết hạn hoặc không giải mã được.'}</p><button onClick={() => setIdentity(null)}>Ẩn thông tin xác minh</button></> : <button disabled={busy} onClick={async () => { const outcome = await run(() => readApi(`/admin/users/${review.userId}/identity`), 'Đã kiểm tra thông tin xác minh; thao tác đã ghi nhật ký.'); if (outcome) setIdentity(outcome.result); }}>Xem thông tin xác minh</button>}</div>}{!owner && review.status === 'pending' && <><label>Ghi chú duyệt<textarea maxLength={1000} value={note} onChange={event => setNote(event.target.value)} /></label><div className="actions"><button className="primary" disabled={busy} onClick={() => run(() => mutateApi(`/admin/reviews/${review.id}/decision`, 'POST', { approve: true, adminNote: note }), 'Đã duyệt và thông báo chủ quán.')}>Phê duyệt</button><button className="danger" disabled={busy || !note.trim()} onClick={() => run(() => mutateApi(`/admin/reviews/${review.id}/decision`, 'POST', { approve: false, adminNote: note }), 'Đã từ chối và gửi lý do.')}>Từ chối</button></div></>}</section>;
}
function AudioPanel({ tasks, busy, run }) {
  const [places, setPlaces] = useState([]);
  const [placeId, setPlaceId] = useState('');
  const [languages, setLanguages] = useState(['vi']);
  const [error, setError] = useState('');
  useEffect(() => { const controller = new AbortController(); readApi('/admin/places', controller.signal).then(items => { setPlaces(items); setPlaceId(String(items[0]?.id || '')); }).catch(caught => { if (!controller.signal.aborted) setError(caught.message); }); return () => controller.abort(); }, []);
  return <><form className="portal-card" onSubmit={event => { event.preventDefault(); run(() => mutateApi('/admin/audio-tasks', 'POST', { placeId: Number(placeId), languages }), 'Đã xếp hàng tạo audio.'); }}><label>Địa điểm<select required value={placeId} onChange={event => setPlaceId(event.target.value)}>{places.map(place => <option key={place.id} value={place.id}>{place.name}</option>)}</select></label><div className="actions">{['vi', 'en', 'zh', 'ja', 'ko', 'fr'].map(language => <label className="checkbox-label" key={language}><input type="checkbox" checked={languages.includes(language)} onChange={event => setLanguages(event.target.checked ? [...languages, language] : languages.filter(item => item !== language))} />{language}</label>)}</div><button className="primary" disabled={busy || !languages.length || !placeId}>Tạo MP3 theo ngôn ngữ</button>{error && <p className="notice">{error}</p>}</form><Table headings={['Địa điểm', 'Trạng thái', 'Tiến độ', 'Điều khiển']}>{tasks.map(task => <tr key={task.id}><td>{places.find(place => place.id === task.placeId)?.name || task.placeId}<small>{task.languages.join(', ')}</small><small>{new Date(task.createdAt).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })}</small></td><td><Badge status={task.status} />{task.error && <small className="danger">{task.error}</small>}</td><td><progress value={task.completed} max={task.languages.length} /> {task.completed}/{task.languages.length}</td><td>{['queued', 'running'].includes(task.status) && <button disabled={busy} onClick={() => run(() => mutateApi(`/admin/audio-tasks/${task.id}/pause`))}>Tạm dừng</button>}{task.status === 'paused' && <button disabled={busy} onClick={() => run(() => mutateApi(`/admin/audio-tasks/${task.id}/resume`))}>Tiếp tục</button>}{['queued', 'running', 'paused'].includes(task.status) && <button className="danger" disabled={busy} onClick={() => run(() => mutateApi(`/admin/audio-tasks/${task.id}/cancel`))}>Hủy</button>}</td></tr>)}</Table></>;
}

function IdentityForm({ data, busy, run }) {
  const [number, setNumber] = useState('');
  return <section className="portal-card compact-form"><h2>Thông tin xác minh tùy chọn</h2><p>Không bắt buộc cho bản demo. Số CCCD được gửi riêng tới backend SmartTour, lưu mã hóa và chỉ Admin có quyền kiểm duyệt được xem. Hết quyền đọc sau 180 ngày, dọn định kỳ mỗi giờ. Không nhập giấy tờ thật khi demo trên máy dùng chung.</p>
    <p>{data?.present ? `Đã lưu: ${data.masked} · hết hạn ${new Date(data.expiresAt).toLocaleDateString('vi-VN')}` : 'Chưa có thông tin xác minh đang hiệu lực.'}</p>
    <form onSubmit={async event => { event.preventDefault(); const outcome = await run(() => mutateApi('/owner/identity', 'PUT', { number }), 'Đã lưu thông tin xác minh được mã hóa.'); if (outcome) setNumber(''); }}><label>Số CCCD · tùy chọn<input type="password" inputMode="numeric" autoComplete="off" pattern="[0-9]{12}" minLength={12} maxLength={12} required value={number} onChange={event => setNumber(event.target.value)} /></label><button className="primary" disabled={busy}>Lưu thông tin xác minh</button></form>
    {data?.present && <button disabled={busy} onClick={() => run(() => mutateApi('/owner/identity', 'DELETE'), 'Đã xóa thông tin xác minh.')}>Xóa thông tin xác minh</button>}
  </section>;
}
