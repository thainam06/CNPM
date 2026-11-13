# Docker kết nối Supabase

File `compose.supabase.yaml` bổ sung cấu hình cho `compose.yaml`, dùng adapter PostgreSQL 6 bảng hiện có trên main. Không dùng provider `Supabase` hoặc `ConnectionStrings__Supabase` của bản thử nghiệm trước đó. `SMARTTOUR_IMPORT_LOCAL` không được adapter hiện tại sử dụng; nhập dữ liệu bằng lệnh riêng bên dưới.

## Mở lại web đã nhập dữ liệu Supabase

Mở Docker Desktop, đợi Engine running. Trong CMD ở thư mục CNPM:

```bat
git pull --ff-only origin main
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Start-Supabase.ps1
```

Script tìm `.env.docker` ở thư mục gốc hoặc `scripts`, build API mới, chép certificate local nếu có, rồi kiểm tra database trước khi khởi động web. Không nhập lại dữ liệu, không xóa bảng/volume, không thay mật khẩu. Chỉ báo PASS khi web trả HTTP 200, API nhận `storage=postgresql` và readiness trả `ready`. Cần mạng để build và kết nối Supabase. Dùng `-EnvFile` nếu cấu hình nằm chỗ khác.

Chỉ kiểm tra database (vẫn build API kiểm tra mới, không khởi động lại web hay chép certificate):

```bat
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Start-Supabase.ps1 -CheckOnly
```

Mã lỗi được in không chứa mật khẩu:
- `CONNECT`: kiểm tra mạng, host/port và Supabase Dashboard; nếu project báo Paused thì Resume rồi thử lại.
- `TLS` / `FILE`: kiểm tra hostname và CA certificate. Giữ `SSL Mode=VerifyFull`; Root Certificate phải là đường dẫn trong container.
- `AUTH`: kiểm tra database password và username Session pooler trong file riêng tư.
- `SCHEMA` / `DATA`: kiểm tra đúng project/schema; không nhập đè hoặc xóa dữ liệu để bỏ qua lỗi.
- `PERMISSION`: role backend thiếu quyền database.
- `POOLER_PROJECT`: đối chiếu chính xác host và username Session pooler trong Connect của đúng project.
- `POOLER_BLOCKED` / `CAPACITY`: pooler đang chặn tạm hoặc hết kết nối; kiểm tra Dashboard và cấu hình trước khi thử lại.
- `READ_ONLY`: kiểm tra trạng thái database/dung lượng và endpoint primary; không xóa dữ liệu.
- Kết quả lỗi SQL có thêm `sqlState` (mã 5 ký tự) và `stage` (`configuration`, `connect-and-read`, `write-probe`). Gửi các mã này để chẩn đoán; công cụ không in nguyên văn lỗi server vì có thể chứa thông tin riêng tư.

`Docker-Demo.ps1` và `Start-PublicDemo.ps1` tự nạp Supabase override khi env có `SUPABASE_CONNECTION_STRING`; có thể chỉ định `-Supabase`. Trước đây hai script bỏ qua override và có thể chuyển về SQLite. Lệnh `docker compose` gõ trực tiếp vẫn cần cả hai file `-f` như dưới.

Sau khi web local đã PASS, mở điện thoại bằng link Cloudflare đang hoạt động. Link Quick Tunnel cũ có thể hết hiệu lực; QR chứa localhost không dùng được trên điện thoại.

## Chuẩn bị

- Giữ nguyên file `.env.docker` có mật khẩu admin, JWT secret và port hiện tại. Thêm `SUPABASE_CONNECTION_STRING` theo mẫu trong `.env.docker.example`, thay thông tin thật trên máy, không commit mật khẩu.
- Trong Supabase SQL Editor, project mới cần chạy `database/postgres/002_smarttour_compact.sql`, sau đó `database/postgres/005_six_table_backend.sql`. Nếu đã có 6 bảng, kiểm tra hướng dẫn `docs/SUPABASE-SETUP.md` trước; không chạy lại script tạo bảng tùy tiện, không chạy phương án 004 đã hủy.
- Nếu muốn giữ dữ liệu hiện tại, sao lưu SQLite trước; xem `scripts/Backup-Sqlite.ps1`. Không xóa Docker volume.

## Chạy từ thư mục gốc CNPM

```bat
git pull --ff-only origin main
docker compose --env-file .env.docker -f compose.yaml -f compose.supabase.yaml build
```

Nếu `.env.docker` nằm trong `scripts`, thay `--env-file .env.docker` bằng `--env-file scripts\.env.docker` trong mọi lệnh. Không đổi project name/volume để vẫn đọc được dữ liệu cũ.

Để chuyển SQLite lên Supabase sau khi sao lưu, dừng API cũ và chạy tác vụ nhập một lần:

```bat
docker compose --env-file .env.docker stop api
docker compose --env-file .env.docker -f compose.yaml -f compose.supabase.yaml run --rm --no-deps -e ImportPostgres=true api
```

Chỉ tiếp tục nếu lệnh nhập báo thành công. Adapter sẽ từ chối ghi đè khi Supabase đã có dữ liệu ứng dụng. Nếu không nhập dữ liệu cũ, bỏ qua bước nhập và dùng dữ liệu đã chuẩn bị trên Supabase.

```bat
docker compose --env-file .env.docker -f compose.yaml -f compose.supabase.yaml up -d --wait --wait-timeout 240
docker compose --env-file .env.docker -f compose.yaml -f compose.supabase.yaml ps
```

Các lần khởi động/build tiếp theo tiếp tục dùng cả hai file `-f`. Chạy base Compose riêng mặc định sẽ quay về SQLite, không tự đồng bộ dữ liệu từ Supabase về SQLite. Ảnh/audio vẫn ở volume Docker; database cloud không thay thế máy chủ web/backend.
