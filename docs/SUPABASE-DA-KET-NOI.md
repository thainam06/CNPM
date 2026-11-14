# Supabase và Admin — kiểm tra ngày 19/11/2026

Admin đang dùng PostgreSQL trên Supabase thật. `/api/health` trả `storage: postgresql`.

## Dữ liệu đã chuyển

Đã nhập snapshot SQLite, đối chiếu toàn bộ trạng thái trước khi commit: 8 địa điểm,
2 tài khoản, 8 ảnh, 8 bản thuyết minh. SQLite gốc vẫn nằm trong volume Docker.
Backup SQLite đã kiểm tra toàn vẹn nằm trong `artifacts/backups/sqlite-20261119-072523/`.

Schema sử dụng: **smarttour_demo**, đúng 6 bảng: `users`, `places`, `place_images`,
`place_narrations`, `visitor_sessions`, `activity_logs`. Các thuộc tính bổ sung nằm
trong JSONB; trạng thái nghiệp vụ phụ nằm ở dòng hệ thống `system:platform-state:v1`.
Không xóa hoặc hiển thị nội dung dòng này khi trình bày audit.

15 bảng cũ đã được kiểm tra đều trống và giữ nguyên trong schema dự phòng
`smarttour_demo_legacy_15`. Không xóa bảng cũ, không ghi đè dữ liệu cloud.
Khi xem sơ đồ, chọn **smarttour_demo**, tránh chọn schema dự phòng.

## Cách sử dụng

1. Mở Docker Desktop, chờ Engine chạy.
2. Tại thư mục dự án, chạy `powershell -ExecutionPolicy Bypass -File scripts/Start-Supabase.ps1`.
3. Mở `http://localhost:8080/admin`, đăng nhập bằng tài khoản Admin đã thống nhất.
4. Supabase Dashboard → Table Editor → schema **smarttour_demo** để xem dữ liệu.
   Chọn view **thiet_bi_truy_cap** để thấy cột **trang_thai**: “Đang hoạt động”
   hoặc “Đã ngắt kết nối”, cùng trình duyệt, hệ điều hành và số tab hoạt động.
   Nếu chưa thấy view mới, tải lại Dashboard. View chỉ đọc, không tạo thêm bảng.
5. Dùng `database/supabase-review/04_demo_admin.sql` trong SQL Editor để xem
   số thiết bị, danh sách trình duyệt, địa điểm và log mà không hiện mật khẩu.

SQL Editor không tự chạy lại câu SELECT: bấm Run để cập nhật. Trang Admin tự
làm mới danh sách. Một trình duyệt mở nhiều tab chỉ tính một thiết bị; đóng tab
gửi leave, mất mạng/đóng đột ngột được loại sau tối đa cửa sổ 45 giây kể từ heartbeat.
Đây là mã trình duyệt ẩn danh, không phải tên máy hoặc địa chỉ MAC.

Trong Table Editor, bấm nút tải lại để tính trạng thái mới; không tự đổi liên tục
khi màn hình đang đứng yên. View tính theo heartbeat trong 45 giây, không lưu cứng
trạng thái online. Khi ứng dụng xóa phiên đã đóng/hết hạn, dòng tương ứng cũng biến
mất khỏi view. Có thể xem bằng SQL: `SELECT * FROM smarttour_demo.thiet_bi_truy_cap;`.
Nếu đổi `PresenceService.WindowSeconds`, sửa cả cửa sổ 45 giây trong migration 008.

Không chạy lại import hay SQL 002/003/004/007 trên database đang có dữ liệu.
Script khởi động chỉ kiểm tra và chạy ứng dụng, không tự import hoặc reset.
Giữ `.env.docker`, chứng chỉ CA và các volume Docker trên máy; không đưa bí mật lên Git.
Ảnh/audio dạng file và khóa mã hóa vẫn dùng volume trên máy chủ. Supabase lưu dữ liệu
quan hệ và metadata; website/API vẫn chạy bằng Docker và cần Internet để gọi cloud.

## Chạy trực tiếp bằng .NET khi Docker/WSL không hoạt động

Cần .NET SDK 10 và Node.js theo yêu cầu frontend. Mở Terminal PowerShell tại
thư mục gốc dự án. Cách này dùng cấu hình local riêng, không tự đọc `.env.docker`.

1. Chạy lệnh cấu hình bên dưới, rồi trả lời từng câu hỏi Host, Port, Database,
   User từ **Connect → Session pooler**. Mật khẩu là mật khẩu database, được nhập
   ẩn. Không dán lệnh `dotnet run` vào câu hỏi Host.

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Configure-Supabase.ps1
   ```

   Chỉ tiếp tục khi thấy `Da luu cau hinh local`. File
   `ShortVideoTranslator.Api/App_Data/postgres.local.json` chứa bí mật, đã được
   Git ignore; không gửi vào chat hoặc commit.

2. Đặt chứng chỉ CA tải từ Supabase tại `prod-ca-2021.crt` trong thư mục gốc.
   Kiểm tra file và lấy đường dẫn Windows tuyệt đối:

   ```powershell
   $supabaseCa = (Resolve-Path .\prod-ca-2021.crt -ErrorAction Stop).Path
   ```

   Không dùng đường dẫn container `/app/App_Data/...` khi chạy trực tiếp.

3. Chạy kiểm tra chỉ đọc, chưa khởi động web hoặc chuyển đổi dữ liệu:

   ```powershell
   dotnet run --project .\ShortVideoTranslator.Api --no-launch-profile -- --ReviewPostgres=true --IncludeConnectionInfo=true --ImportPostgres=false "--Storage:PostgresRootCertificate=$supabaseCa"
   ```

   Nếu có lỗi TLS/AUTH/CONNECT/SCHEMA/DATA, xử lý theo báo cáo trước khi tiếp tục.
   Không chạy lại import hoặc migration để chữa lỗi kết nối.

4. Khi review thành công, khởi động API trong cùng Terminal:

   ```powershell
   dotnet run --project .\ShortVideoTranslator.Api --launch-profile http -- --Storage:Provider=Postgres --ImportPostgres=false "--Storage:PostgresRootCertificate=$supabaseCa"
   ```

   Giữ Terminal chạy sau dòng `Now listening on: http://localhost:5132`.
   Lệnh này khởi động ứng dụng thật và có thể ghi dữ liệu khi sử dụng Admin;
   không còn là bước kiểm tra chỉ đọc.

5. Mở Terminal thứ hai tại thư mục gốc, chạy từng lệnh:

   ```powershell
   cd .\ShortVideoTranslator.Frontend
   npm ci
   npm run dev
   ```

   Mở `http://localhost:5173/admin`. Kiểm tra
   `http://localhost:5132/api/health` phải trả `storage: postgresql`.
   Nếu trả `sqlite`, đó là dữ liệu local, chưa phải Admin kết nối Supabase.

Tài khoản Admin dùng tài khoản đã có trên database. Bật một API cho mỗi nguồn
dữ liệu, không chạy đồng thời bản SQLite và PostgreSQL trên cổng 5132.
Các bước trên không thay thế backup/restore hoặc kiểm thử nhiều thiết bị.

## Bằng chứng kiểm tra

- TLS VerifyFull với CA Supabase; không bỏ xác thực chứng chỉ.
- Đăng nhập Admin, đọc 8 địa điểm bằng API có phân quyền.
- Gửi 3 phiên thử thuộc 2 mã trình duyệt; kiểm tra bản ghi trong Supabase và
  danh sách Admin, xác nhận gộp tab; gửi leave rồi kiểm tra các phiên thử đã bị xóa.
- Báo cáo kiểm tra cục bộ: `artifacts/supabase-work-20261119/verification.json`.
- Sau khi restart API và tạo lại web: health/ready đạt, đăng nhập Admin vẫn thành
  công, cả API công khai và Admin đều đọc 8 địa điểm. Review cuối trả `REVIEW_OK`,
  không lỗi khóa ngoại/cấu trúc/envelope. Bộ kiểm thử PostgreSQL cô lập đạt 20/20,
  gồm trường hợp migration từ chối schema có dữ liệu và giữ nguyên bản ghi.

Chỉ mở `compose.yaml` sẽ chọn SQLite. Khi cần dùng cloud, sử dụng script trên hoặc
thêm `-f compose.supabase.yaml` cùng `--env-file .env.docker`. Không đổi provider
qua lại khi đang nhận dữ liệu vì hai nguồn không tự đồng bộ hai chiều.
