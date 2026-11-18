# Supabase — thiết kế được chốt: 6 bảng

Cập nhật 29/10/2026: giữ schema **smarttour_demo** với 6 bảng:
users, places, place_images, place_narrations, visitor_sessions, activity_logs.

## File cần dùng

- `database/postgres/002_smarttour_compact.sql`: tạo mới bản 6 bảng, mỗi bảng 6–8 cột.
- `database/postgres/003_compact_existing_tables.sql`: chuyển bản 6 bảng nhiều cột cũ sang bản rút gọn; giữ dữ liệu phụ trong JSONB.
- `docs/DATABASE-6-TABLES.md`: cấu trúc và giới hạn.
- `docs/PRD-6-TABLES-UPDATE.md`: nội dung cập nhật PRD và ERD để nhóm đưa vào báo cáo.

**Không chạy 004_align_demo_with_prd.sql**: phương án 15 bảng đã hủy theo lựa chọn cuối.
File 001 là phương án cũ 27 bảng thuộc schema smarttour; không dùng nó để tạo bản rút gọn.

## Trạng thái hiện tại

Kiểm tra ngày 15/11/2026: kết nối Supabase thành công, schema smarttour_demo đủ 6 bảng,
migration 005 đã áp dụng, dữ liệu đã có (8 places, 8 place_images, 8 place_narrations,
1 users, 10 activity_logs, 0 visitor_sessions). `scripts/Start-Supabase.ps1` báo PASS;
API chạy với `storage=postgresql` và `/api/places` trả 8 địa điểm từ Supabase.
Không cần chạy lại SQL. Hướng dẫn cho thành viên nhóm: `HUONG-DAN-XEM-SUPABASE.md` (thư mục gốc CNPM).

Chưa kiểm tra thao tác ghi (đăng nhập admin, thêm/sửa/xóa) và chưa xác nhận toàn bộ 28 UC
hoạt động bằng Supabase; mapping phụ dùng JSONB envelope và giới hạn kiểm chứng được liệt kê trong tài liệu cập nhật PRD.

## Kiểm tra và vận hành adapter hiện có

1. Sao lưu SQLite và giữ password hash/security stamp hiện có.
2. Lấy cấu hình PostgreSQL trong Connect; Session pooler dùng được cho môi trường IPv4.
   Lưu mật khẩu và connection string ở cấu hình local bị Git ignore; không gửi vào chat hoặc React.
3. Dùng adapter `PostgresPlatformStore`/`PostgresStateCodec` đã có. Bật Postgres bằng
   Supabase Compose override; không viết adapter mới hoặc import lại dữ liệu đã tồn tại.
4. Kiểm thử đăng nhập, CRUD, thuyết minh, thống kê/consent và backup/restore trước chuyển demo.

Hướng dẫn kết nối chính thức: https://supabase.com/docs/guides/database/connecting-to-postgres

Database cloud không thay thế máy chủ C#. Quick Tunnel vẫn cần laptop và Docker đang chạy.
Không xóa schema cũ hoặc Docker volume khi chưa sao lưu và xác nhận dữ liệu không còn dùng.

## Bàn giao theo yêu cầu nhóm

Bộ SQL chỉ đọc, template Session pooler và lệnh `-ReviewOnly` nằm tại
[database/supabase-review/README.md](../database/supabase-review/README.md).
Ngày 18/11 kiểm thử trên PostgreSQL riêng; không có credentials trong cloud để kiểm tra lại project online.

## Schema 15 bảng không tương thích

Dùng [quy trình PLAN → backup → archive/migrate → verify](SUPABASE-15-TO-6.md).
Không chạy 003 trên schema 15 bảng hoặc xóa 9 bảng phụ. Công cụ kiểm tra trước và
chặn dữ liệu nghiệp vụ chưa mapping; trạng thái online phải được xác minh riêng.
