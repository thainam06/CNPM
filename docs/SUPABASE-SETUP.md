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

Ảnh Schema Visualizer cho thấy người dùng đã có bản rút gọn 6 bảng trong smarttour_demo.
Không cần chạy lại SQL nếu cấu trúc đang đúng. Chưa có kết nối độc lập để kiểm tra dữ liệu
thực trên Supabase. Website vẫn dùng SQLite, chưa có adapter PostgreSQL và chưa import dữ liệu thật.

Việc tạo bảng chưa làm Frontend/Backend tự chuyển sang database cloud. Chưa xác nhận toàn bộ
28 UC đã hoạt động bằng Supabase; các mapping phụ còn thiếu được liệt kê trong tài liệu cập nhật PRD.

## Bước tiếp theo khi kết nối Backend

1. Sao lưu SQLite và giữ password hash/security stamp hiện có.
2. Lấy cấu hình PostgreSQL trong Connect; Session pooler dùng được cho môi trường IPv4.
   Lưu mật khẩu và connection string ở cấu hình local bị Git ignore; không gửi vào chat hoặc React.
3. Hoàn thiện mapping và adapter Data trước khi đổi Storage:Provider.
4. Kiểm thử đăng nhập, CRUD, thuyết minh, thống kê/consent và backup/restore trước chuyển demo.

Hướng dẫn kết nối chính thức: https://supabase.com/docs/guides/database/connecting-to-postgres

Database cloud không thay thế máy chủ C#. Quick Tunnel vẫn cần laptop và Docker đang chạy.
Không xóa schema cũ hoặc Docker volume khi chưa sao lưu và xác nhận dữ liệu không còn dùng.
