# Docker kết nối Supabase

File `compose.supabase.yaml` bổ sung cấu hình cho `compose.yaml`, dùng adapter PostgreSQL 6 bảng hiện có trên main. Không dùng provider `Supabase` hoặc `ConnectionStrings__Supabase` của bản thử nghiệm trước đó. `SMARTTOUR_IMPORT_LOCAL` không được adapter hiện tại sử dụng; nhập dữ liệu bằng lệnh riêng bên dưới.

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
