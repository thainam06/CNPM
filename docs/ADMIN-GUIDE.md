# Hướng dẫn chức năng Admin SmartTour Guide

Đối chiếu mã nguồn ngày 18/10/2026. Truy cập http://localhost:5173/admin khi frontend và API đang chạy. Menu và thao tác phụ thuộc quyền của tài khoản; tài khoản có đủ quyền thấy 11 mục dưới đây.

| Mục | Chức năng |
| --- | --- |
| Tổng quan | Số địa điểm, địa điểm công khai, tài khoản, yêu cầu chờ duyệt, sự kiện khách đã đồng ý, thiết bị hoạt động trong 5 phút và hàng đợi thống kê. Xuất CSV nếu có quyền. |
| Địa điểm | Thêm, sửa, xóa; nội dung tiếng Việt, phân loại, địa chỉ, tọa độ và nguồn kiểm chứng; trạng thái demo/công khai, gán chủ quán, bán kính kích hoạt và ưu tiên audio. Quản lý ảnh bìa, điểm crop, nguồn ảnh, bộ ảnh tự chuyển 3 giây và thực đơn. Có trợ lý AI trong trình sửa. |
| Tài khoản | Tạo tài khoản, đổi vai trò, khóa/mở khóa và xóa theo quyền. Bảo vệ tài khoản đang dùng và tài khoản quản trị cao nhất. |
| Vai trò | Tạo, sửa, xóa vai trò tùy chỉnh và chọn quyền. Vai trò hệ thống được bảo vệ; không được cấp quyền vượt quyền của người thao tác. |
| Kiểm duyệt | Xem và duyệt/từ chối đăng ký chủ quán hoặc đề xuất địa điểm. Lưu ghi chú, xem thông tin xác minh khi có quyền. Chủ quán nhận kết quả trong cổng riêng; hồ sơ bị từ chối có thể gửi yêu cầu xem xét lại kèm ghi chú. |
| Bản dịch thuyết minh | Chọn địa điểm và ngôn ngữ Anh, Trung, Nhật, Hàn, Pháp; xem nội dung nguồn, xem trước bản dịch, sửa/lưu nháp, duyệt và tạo MP3 trước khi công bố, nghe bản công bố hoặc thu hồi theo quyền. Tiếng Việt dùng nội dung gốc. |
| Tác vụ audio | Tạo MP3 cho địa điểm với tối đa 6 ngôn ngữ qua Edge-TTS. Xem tiến độ, kết quả/lỗi, tạm dừng, tiếp tục, hủy. Giọng Việt vi-VN-HoaiMyNeural. |
| Nhật ký | Xem thời gian, hành động, tài nguyên và người thực hiện để đối chiếu thay đổi. |
| Giám sát vị trí | Xem phiên ẩn danh của khách đã đồng ý riêng, trong 1/6/24 giờ. Tọa độ làm tròn khoảng 100 m, lưu tối đa 24 giờ; không tự theo dõi tất cả khách. |
| Kiểm tra hệ thống | Kiểm tra kho dữ liệu, cấu hình worker, gói bản đồ, cấu hình AI/thời tiết và số MP3 đúng nội dung theo ngôn ngữ. Cờ cấu hình không chứng minh dịch vụ ngoài kết nối thành công. |
| Mật khẩu | Đổi mật khẩu bằng mật khẩu hiện tại; mật khẩu mới tối thiểu 7 ký tự, vô hiệu phiên cũ. |

## Quy trình trình bày demo

1. Tổng quan → Địa điểm: giới thiệu dữ liệu 5 địa điểm Quận 1, ảnh, nguồn, địa chỉ, nội dung và trạng thái.
2. Bản dịch thuyết minh → Tác vụ audio: giải thích dịch nội dung trước khi tạo giọng nói, mở MP3 đã có để nghe.
3. Kiểm duyệt: trình bày đề xuất của chủ quán, ghi chú duyệt/từ chối và thông báo kết quả trong `/owner`.
4. Tài khoản → Vai trò → Nhật ký: giới thiệu phân quyền và dấu vết thao tác.
5. Tổng quan → Giám sát vị trí → Kiểm tra hệ thống: phân biệt thống kê có đồng ý, chia sẻ vị trí riêng và kiểm tra khả năng chạy demo.

## Giới hạn thực tế

- AI Gemini/ProxyPal và thời tiết OpenWeather chưa có cấu hình dịch vụ thật trong môi trường hiện tại. Giao diện báo chưa sẵn sàng; có thể tự nhập nội dung.
- 5 địa điểm đã có 30 MP3 cho 6 ngôn ngữ. Bản dịch mẫu là bản nháp hỗ trợ bằng AI, chưa thay thế kiểm duyệt chuyên môn.
- Nhãn điều hướng, biểu mẫu và thao tác chung có 6 ngôn ngữ; một số hướng dẫn vận hành và thông báo backend vẫn tiếng Việt.
- GPS đã có kiểm thử bằng dữ liệu giả lập, chưa xác nhận ngoài thực địa. Kho JSON phục vụ demo một tiến trình; chưa kiểm thử tải hoặc triển khai production.
- Endpoint sao lưu có phân quyền là API riêng, chưa có giao diện sao lưu/khôi phục hoàn chỉnh.

## Kiểm tra mới nhất

`dotnet build` thành công, không warning/error; `dotnet test --no-build`: 105/105. Frontend: 34/34 bài, lint sạch và production build thành công; còn cảnh báo chunk bản đồ lớn. Luồng thông báo và gửi lại đăng ký dùng tài khoản giả lập, kho dữ liệu riêng để không sửa tài khoản/địa điểm thật.
