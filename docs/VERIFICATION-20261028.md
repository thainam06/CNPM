# Kiểm chứng ngày 28/10/2026

- .NET build thành công: 0 warning, 0 error sau khi restore NuGet ngoài sandbox.
- Backend: 117/117 test đạt. Bao gồm nhập JSON sang SQLite, rollback, 32 kết nối cập nhật không mất dữ liệu, bảo vệ endpoint, metadata thiết bị, sắp xếp lại các dòng unique và giữ nguyên lịch sử SQL khi thêm sự kiện.
- Frontend: lint đạt, 47/47 test đạt; build local và build production trong Docker thành công. Vite còn cảnh báo chunk MapLibre lớn hơn 500 kB; bản đồ offline đã lazy-load.
- Docker API/web/tunnel healthy, SQLite integrity_check = ok, đủ 8 POI. Tài khoản hiện tại được giữ nguyên.
- Link HTTPS tại thời điểm kiểm tra: https://carpet-itself-nirvana-stopping.trycloudflare.com. Đây là link tạm và có thể đổi.
- `scripts/Verify-PublicDemo.ps1`: 12 client giả lập gửi đồng thời qua Cloudflare, đủ 12 heartbeat và nhãn Chrome/Windows trong SQL; xóa toàn bộ các mã thử qua API forget. Đây không phải thử 12 điện thoại vật lý hoặc kiểm thử tải lớn.
- Browser đăng nhập Admin thành công, nhìn thấy bảng SQLite và cột loại thiết bị/trình duyệt. Các bản ghi cũ chưa gửi metadata hiển thị Other/Unknown.
- `scripts/Backup-Demo.ps1` chạy thành công, lưu App_Data, DataProtection-Keys, .env.docker tại `artifacts/backups/20261028-110514`; API khởi động lại. Chưa chạy khôi phục backup sang máy khác.
- Theo yêu cầu cuối: gợi ý tự chạy khi mở trang, không cần nút bật. Browser mới mở đã hiển thị "Đang tự lấy vị trí…" mà không bấm nút gợi ý. Minh chứng `artifacts/demo-20261028/automatic-nearby.png`.
- Môi trường browser kiểm thử chưa trả tọa độ GPS và báo timeout sau thời gian chờ. Không tuyên bố đã xác minh gợi ý GPS ngoài thực địa. Logic di chuyển/đổi thứ tự gợi ý, tọa độ cũ/sai, hủy callback và tắt watch được kiểm thử bằng adapter giả lập. Trình duyệt vẫn bắt buộc cấp quyền vị trí lần đầu.
- Chưa triển khai domain cố định/VPS; dùng Quick Tunnel theo lựa chọn chủ dự án. Chưa kiểm thử nhiều API replica hoặc tái chạy toàn bộ TTS/offline trong đợt này.
- Không commit/push trong đợt này.
