# Hướng dẫn xem dữ liệu Supabase và chạy dự án

Tài liệu cho thành viên nhóm. Không chứa mật khẩu; mật khẩu chỉ nằm trong file `.env.docker` trên máy (đã bị Git ignore, không gửi qua chat).

## 1. Trạng thái đã kiểm tra (19/11/2026)

Chi tiết lần chuyển dữ liệu thật và hướng dẫn xem thiết bị: [SUPABASE-DA-KET-NOI](docs/SUPABASE-DA-KET-NOI.md).

| Hạng mục | Kết quả |
|---|---|
| Kết nối Supabase (Session pooler, SSL VerifyFull) | OK |
| Schema `smarttour_demo` đủ 6 bảng | OK |
| Migration `005_six_table_backend.sql` đã áp dụng | OK |
| Script `scripts/Start-Supabase.ps1` | **PASS** |
| `/api/health` | `storage = postgresql`, `status = ready` |
| `/api/places` | trả đúng 8 địa điểm từ Supabase |

Dữ liệu hiện có trên Supabase:

| Bảng | Số dòng | Ghi chú |
|---|---|---|
| `places` | 8 | Chợ Bến Thành, Dinh Độc Lập, Bưu điện Trung tâm Sài Gòn, Phố đi bộ Nguyễn Huệ, Nhà hát Thành phố, Nhà thờ Đức Bà Sài Gòn, Bảo tàng Mỹ thuật TP.HCM, Công viên Tao Đàn |
| `place_images` | 8 | |
| `place_narrations` | 8 | |
| `users` | 2 | giữ nguyên tài khoản từ SQLite |
| `activity_logs` | thay đổi theo hoạt động | gồm 1 dòng hệ thống `system:platform-state:v1` |
| `visitor_sessions` | thay đổi theo truy cập | phiên đang hoạt động, tự loại khi leave/hết hạn |

Đã kiểm tra đăng nhập Admin, đọc địa điểm và heartbeat/leave ghi vào Supabase thật.
15 bảng cũ trống được giữ tại schema `smarttour_demo_legacy_15`; chọn `smarttour_demo` để xem đúng 6 bảng.

## 2. Xem dữ liệu trên Supabase (trình duyệt)

1. Vào https://supabase.com/dashboard, đăng nhập, chọn project của nhóm.
   - Không thấy project → nhờ người tạo mời vào ở **Project Settings → Team**.
   - Project báo **Paused** → bấm **Resume** (gói miễn phí tự tạm dừng khi ~1 tuần không ai dùng; nên kiểm tra trước buổi demo).
2. **Table Editor** (menu trái) → ô chọn schema phía trên đổi `public` thành **`smarttour_demo`** → bấm từng bảng để xem.
3. **SQL Editor** → dán câu sau → **Run** để đếm nhanh cả 6 bảng:

```sql
select 'users' t, count(*) from smarttour_demo.users
union all select 'places', count(*) from smarttour_demo.places
union all select 'place_images', count(*) from smarttour_demo.place_images
union all select 'place_narrations', count(*) from smarttour_demo.place_narrations
union all select 'visitor_sessions', count(*) from smarttour_demo.visitor_sessions
union all select 'activity_logs', count(*) from smarttour_demo.activity_logs;
```

Chỉ dùng SQL Editor để **xem** (`select`). Không chạy `delete`, `drop`, `truncate` hay chạy lại file SQL tạo bảng.

Lỗi "Error getting user profile from external provider" khi đăng nhập bằng GitHub: thử tab ẩn danh, kiểm tra email GitHub đã verified, hoặc vào GitHub → Settings → Applications → Authorized OAuth Apps → Revoke Supabase rồi đăng nhập lại.

## 3. Chạy web với Supabase trên máy

Cần: Docker Desktop (Engine running, Linux containers), mạng Internet, file `.env.docker` có dòng `SUPABASE_CONNECTION_STRING=...` và file `supabase-ca.crt` ở thư mục gốc CNPM.

```bat
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Start-Supabase.ps1
```

Script lần lượt: kiểm tra Docker → kiểm tra `.env.docker` → build API → chép chứng chỉ SSL → **kiểm tra Supabase** → build web → khởi động API + web → gọi thử `/api/health`, `/api/health/ready`, trang chủ.

Chỉ khi tất cả đều đạt, terminal mới in:

```
PASS: web + PostgreSQL are ready. Open http://localhost:8080 (admin: http://localhost:8080/admin).
```

Bước nào lỗi thì script dừng và in mã lỗi (`CONNECT`, `AUTH`, `TLS`, `SCHEMA`, `POOLER_PROJECT`, ...). Ý nghĩa từng mã: xem `docs/DOCKER-SUPABASE.md`. Script **không** import, xóa hay ghi đè dữ liệu cloud.

Chỉ kiểm tra database, không khởi động web:

```bat
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Start-Supabase.ps1 -CheckOnly
```

Sau khi PASS:
- Trang chính: http://localhost:8080
- Trang admin: http://localhost:8080/admin (giữ nguyên tài khoản Admin đã thống nhất; biến trong `.env.docker` chỉ khởi tạo tài khoản khi chưa có)
- Mở cho điện thoại: tạo Cloudflare tunnel mới trỏ tới `http://localhost:8080` (link tunnel cũ có thể đã hết hạn). Laptop và Docker phải chạy suốt buổi demo.

## 4. Lưu ý bảo mật

- Không commit/gửi `.env.docker`; người khác cần chạy thì tự lấy connection string ở nút **Connect → Session pooler** của project và mật khẩu database từ chủ project.
- `supabase-ca.crt` là chứng chỉ công khai của Supabase, không phải bí mật.

## 5. Bộ kiểm tra bàn giao mới (18/11/2026)

Xem [database/supabase-review/README.md](database/supabase-review/README.md) để có
SQL xem cấu trúc, số dòng, dữ liệu mẫu chọn lọc, lỗi liên kết và template Session pooler.
Trong repo dùng tên `users` và `places`; không tạo `user`/`port` chỉ để khớp cách gọi trong tin nhắn.

Muốn báo cáo chỉ đọc mà không khởi động app:

```bat
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Start-Supabase.ps1 -ReviewOnly
```

Thêm `-IncludeConnectionInfo` nếu muốn hiện Host/Port/Database/User cho nhóm; không hiện
password. Review dùng transaction READ ONLY, khác `-CheckOnly` có write-probe rollback.
Kết quả kiểm thử 18/11 là trên PostgreSQL thử nghiệm; số liệu mục 1 vẫn là báo cáo 15/11,
chưa được kiểm tra lại trên Supabase online trong lượt này.

## 6. Máy Windows mới và lỗi TLS/WSL

Xem [hướng dẫn xử lý theo từng bước](docs/SUPABASE-WINDOWS-TROUBLESHOOTING.md).
Có thể dùng `-CertificateFile` để chọn rõ CA cho `-ReviewOnly` hoặc `-CheckOnly`;
script mount chỉ đọc và tự áp dụng đường dẫn CA trong container, giữ VerifyFull.
