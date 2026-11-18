# Mẫu báo cáo kiểm tra Supabase gửi Triết

Đây là **mẫu chưa điền kết quả**, không phải bằng chứng đã kiểm tra database online.
Copy thành báo cáo riêng và điền từ kết quả chạy thực tế. Không lấy số liệu lịch sử
hoặc dữ liệu fixture PostgreSQL/CI để điền vào kết quả Supabase của nhóm.

## Thông tin lượt kiểm tra

| Nội dung | Giá trị cần điền |
|---|---|
| Người kiểm tra | Chưa điền |
| Thời gian, múi giờ | Chưa điền |
| Project và môi trường (thử nghiệm/online của nhóm) | Chưa xác nhận |
| Commit đang sử dụng | Chưa điền; lấy bằng `git rev-parse --short HEAD` |
| Schema thực tế | Chưa xác nhận; schema backend yêu cầu là `smarttour_demo` |
| Phương pháp | SQL Editor / `-ReviewOnly` / kiểm tra ứng dụng — chọn bước đã thực hiện |
| Quyền người thực thi, giới hạn RLS nếu có | Chưa xác nhận |

## 1. Sáu bảng và số bản ghi

Lấy tên/schema/cột từ [01_check_schema.sql](01_check_schema.sql), số dòng từ
[02_view_data.sql](02_view_data.sql) hoặc section `counts-1` của báo cáo JSON.
Tên trong repo là `users`, `places`; cần xác nhận với Triết chữ `port` có ý chỉ
bảng địa điểm hay không. Không tạo thêm bảng để khớp cách gọi trong tin nhắn.

| Bảng trong `smarttour_demo` | Tìm thấy | Số dòng thực tế | Cột/kiểu đúng theo repo | Bằng chứng |
|---|---|---|---|---|
| users | Chưa kiểm tra | Chưa đo | Chưa kiểm tra | Chưa có |
| places | Chưa kiểm tra | Chưa đo | Chưa kiểm tra | Chưa có |
| place_images | Chưa kiểm tra | Chưa đo | Chưa kiểm tra | Chưa có |
| place_narrations | Chưa kiểm tra | Chưa đo | Chưa kiểm tra | Chưa có |
| visitor_sessions | Chưa kiểm tra | Chưa đo | Chưa kiểm tra | Chưa có |
| activity_logs | Chưa kiểm tra | Chưa đo | Chưa kiểm tra | Chưa có |

Không dùng số `0` thay cho “chưa đo”. Không thấy bảng có thể do thiếu quyền hoặc
chọn sai schema/project; chưa đủ cơ sở kết luận bảng không tồn tại.

## 2. Toàn vẹn dữ liệu

Chạy [03_check_integrity.sql](03_check_integrity.sql). Mỗi dòng `violations` phải
bằng 0; ghi lại tên kiểm tra và số lỗi nếu khác 0, không tự xóa bản ghi để làm xanh.

| Hạng mục | Kết quả thực tế / lỗi | Bằng chứng |
|---|---|---|
| Ảnh, narration, owner và log không mồ côi | Chưa kiểm tra | Chưa có |
| View/play có consent và device hợp lệ | Chưa kiểm tra | Chưa có |
| Ngôn ngữ hợp lệ; không trùng bản approved/ảnh bìa | Chưa kiểm tra | Chưa có |
| Nội dung narration, tọa độ và thời gian phiên hợp lệ | Chưa kiểm tra | Chưa có |
| Năm FK deferrable theo migrations 002/005 | Chưa kiểm tra | Chưa có |
| State envelope của backend | Chưa kiểm tra | Chưa có |
| Cảnh báo ảnh gallery / nguồn tiếng Việt | Chưa kiểm tra | Chưa có |

State envelope chỉ cần báo số lượng, không xuất `activity_logs.details`.
Thiếu gallery hoặc `audio_path` không tự chứng minh ảnh bìa/MP3 trên ứng dụng hỏng;
đối chiếu thêm cách codec lưu file và metadata.

## 3. Kết nối và ứng dụng

Điền riêng từng bước, không suy ra bước sau đạt chỉ từ kết quả `REVIEW_OK`.

| Kiểm tra | Kết quả | Bằng chứng đã lọc thông tin nhạy cảm |
|---|---|---|
| `-ReviewOnly`: mã kết quả và exit code | Chưa chạy | Chưa có |
| `transaction_read_only = on` | Chưa kiểm tra | Chưa có |
| `/api/health`: provider `postgresql` | Chưa kiểm tra | Chưa có |
| `/api/health/ready`: trạng thái ready | Chưa kiểm tra | Chưa có |
| `/api/places`: đối chiếu ID/tên/số địa điểm với SQL | Chưa kiểm tra | Chưa có |
| Đăng nhập tài khoản thử được cấp | Chưa thử | Chưa có |
| Thêm/sửa/xóa và đọc lại trên kho thử được phép | Chưa thử | Chưa có |
| Ảnh/MP3 đúng địa điểm, ngôn ngữ và truy cập được | Chưa thử | Chưa có |

Các bước ghi dữ liệu phải dùng kho thử hoặc có sự cho phép của người quản trị.
Không import/reset dữ liệu online hoặc thay mật khẩu tài khoản thật để kiểm tra.

## 4. Thông tin Session pooler bàn giao riêng

- Điền [SESSION-POOLER.example.txt](SESSION-POOLER.example.txt) từ **Connect → Session pooler**.
- Host/Port/Database/User đã đối chiếu đúng project: **Chưa xác nhận**.
- Mật khẩu đã cấp qua kênh riêng an toàn: **Chưa xác nhận**; không điền mật khẩu vào báo cáo.
- Không đính kèm `.env.docker`, connection string có password, password hash,
  token, bản dump database hoặc toàn bộ JSONB state.
- Chỉ đính kèm ảnh/JSON đã kiểm tra nội dung và phù hợp quyền chia sẻ của nhóm.

## 5. Kết luận gửi Triết

> Kiểm tra lúc ... trên project/môi trường ..., commit ... .
> Schema thực tế: ...; số bảng đã xác nhận: .../6.
> Dữ liệu và kiểm tra liên kết: ... .
> Provider/readiness/catalog của ứng dụng: ... .
> Đăng nhập/thao tác ghi/MP3: đã thử hoặc chưa thử ... .
> Thông tin pooler gửi riêng; mật khẩu không nằm trong Git.
> Lỗi còn lại, người xử lý và bước tiếp theo: ... .

Trước khi gửi, thay các chỗ chưa điền bằng kết quả hoặc ghi rõ “chưa kiểm tra”.
Không ghi “đồ án đã chạy đầy đủ trên Supabase” khi mới có kiểm tra SELECT/schema.
