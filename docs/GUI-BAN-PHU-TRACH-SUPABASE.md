# Gửi bạn phụ trách Supabase

Backend hiện dùng ASP.NET Core 10 và Npgsql,
truy cập PostgreSQL qua Session pooler. React gọi API C#, không truy cập database
bằng service_role/anon key. Không cần đổi Supabase Auth hay gửi API key.

## Nếu project đang có 15 bảng

Đã có công cụ PLAN/APPLY có archive và rollback trong [SUPABASE-15-TO-6.md](SUPABASE-15-TO-6.md).
Chạy PLAN trước. Công cụ chỉ tự xử lý thiết kế 004 có snapshot compact có thẩm quyền
hoặc schema rỗng; các workflow rows chưa mapping sẽ chặn chuyển đổi. Không DROP schema
để tạo lại, không chạy lại 004. Admin đọc/ghi sau migration đã được thử trên PostgreSQL
riêng; chưa có credentials để áp dụng/kiểm chứng trên project online của nhóm.

## Bạn cần cung cấp

1. Chạy file `database/postgres/006_kiem_tra_cau_truc.sql` trong SQL Editor.
   Chỉ SELECT, không sửa dữ liệu. Xuất kết quả CSV hoặc chụp phần tên bảng/cột/khóa ngoại.
2. Cho biết project/schema nào dành cho SmartTour và đã có dữ liệu thật chưa.
3. Lấy Host, Port, Database, User tại Connect → Direct / Connection string → Session pooler.
   Bốn giá trị này có thể gửi cho nhóm; mật khẩu database truyền riêng an toàn và
   nhập trực tiếp bằng scripts/Configure-Supabase.ps1, không dán vào chat hoặc source.

## Cấu trúc Backend đang cần

Namespace `smarttour_demo`, 6 bảng và các cột sau (tên/kiểu dữ liệu phải đúng):

| Bảng | Cột |
|---|---|
| users | id text, username text, display_name text, password_hash text, role text, enabled boolean, settings jsonb |
| places | id bigint, owner_id text, name text, address text, latitude double precision, longitude double precision, introduction text, settings jsonb |
| place_images | id bigint, place_id bigint, url text, caption text, ordinal integer, is_cover boolean, metadata jsonb |
| place_narrations | id text, place_id bigint, language_code text, narration_text text, audio_path text, status text, version text, metadata jsonb |
| visitor_sessions | id text, device_id text, device_info jsonb, started_at timestamptz, last_seen_at timestamptz, ended_at timestamptz |
| activity_logs | id text, kind text, user_id text, device_id text, place_id bigint, consent boolean, details jsonb, created_at timestamptz |

Nếu chưa có bảng/dữ liệu: xem file 002_smarttour_compact.sql để tạo cấu trúc,
sau đó 005_six_table_backend.sql để chuẩn bị FK deferrable. Không chạy 004 (15 bảng).
Nếu đã có schema khác hoặc dữ liệu, gửi kết quả kiểm tra trước; không xóa/đổi bảng
của bạn để ép khớp. Adapter phải được điều chỉnh hoặc viết migration có đối chiếu.

## Dữ liệu đặc biệt cần biết

Nhằm giữ đúng 6 bảng, reviews, roles tùy chỉnh, menu, thông báo, refresh sessions,
task audio và metadata nền tảng được lưu trong một bản ghi JSONB có id
`system:platform-state:v1` ở activity_logs. Đây là bản ghi hệ thống, không phải
lượt xem hay audit của người dùng. Không xóa/sửa tùy tiện hoặc cấp đọc cho browser.
Những quan hệ bên trong JSONB do Backend xác thực, không có FK vật lý riêng.

Import hiện chỉ nhận database đích trống. Nó từ chối ghi đè dữ liệu đã có và so sánh
toàn bộ PlatformState trước COMMIT; lỗi sẽ rollback. Database đã được nhóm nhập
trước đó cần kiểm tra tương thích bằng adapter, không chạy import lại.

## Nhóm chạy sau khi có cấu hình

Tại `C:\CNPM\ShortVideoTranslator`:

```powershell
.\scripts\Configure-Supabase.ps1
docker compose --env-file .env.docker -f compose.yaml -f compose.supabase.file.yaml build api
docker compose --env-file .env.docker -f compose.yaml -f compose.supabase.file.yaml run --rm --no-deps -e CheckPostgresSchema=true api
```

Lệnh kiểm tra chỉ đọc schema và số dòng; không chuyển API đang chạy.
Chỉ sau khi schema đạt, backup SQLite và xác nhận database đích trống mới nhập:

```powershell
.\scripts\Backup-Demo.ps1
docker compose --env-file .env.docker stop api
docker compose --env-file .env.docker -f compose.yaml -f compose.supabase.file.yaml run --rm --no-deps -e ImportPostgres=true api
```

Nếu import thất bại, khởi động lại bản SQLite bằng `docker compose --env-file .env.docker up -d api`.
Nếu import thành công, chạy:

```powershell
docker compose --env-file .env.docker -f compose.yaml -f compose.supabase.file.yaml up -d --wait api web
```

Kiểm tra `/api/health` có storage=postgresql, đăng nhập Admin, CRUD địa điểm,
vai trò/kiểm duyệt, thuyết minh, thiết bị online, restart vẫn còn dữ liệu.
Ảnh/MP3/khóa mã hóa vẫn thuộc Docker volume nên không xóa volume.
Quay lại SQLite không tự đồng bộ các thay đổi mới đã ghi lên Supabase.
