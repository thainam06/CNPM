# Sử dụng SmartTour Guide offline

## Chuẩn bị trên máy demo

1. Mở Docker Desktop và chạy `docker compose --env-file .env.docker up -d --wait`.
2. Khi cần nhập lại tài nguyên đã chuẩn bị trên máy này, chạy `./scripts/Prepare-DockerOffline.ps1` trong PowerShell. Script cần các MP3 và bản đồ trong App_Data local; chỉ nhập tài nguyên công khai đã kiểm tra SHA-256, không nhập tài khoản hay platform.json. Máy mới clone GitHub chưa có các tài nguyên này: cần tạo MP3 qua Admin và chuẩn bị bản đồ bằng `scripts/Prepare-MapPack.ps1` trước.
3. Mở http://localhost:8080 trong đúng trình duyệt sẽ dùng demo. Không dùng cửa sổ ẩn danh.
4. Chọn ngôn ngữ, xuống **Tiện ích khám phá**, bấm **Tải gói offline đầy đủ**. Chờ báo đã lưu 8 địa điểm, 16 ảnh, 8 MP3 và bản đồ.
5. Bấm **Kiểm tra gói trên thiết bị**. Chọn bản đồ **Offline · gói Quận 1** để chủ động dùng dữ liệu local, hoặc Hybrid để tự chuyển khi trình duyệt báo mất mạng/bản đồ online lỗi.
6. Lặp lại cho ngôn ngữ cần dùng. Hỗ trợ sáu ngôn ngữ nhưng giữ tối đa ba ngôn ngữ trên mỗi trình duyệt; ngôn ngữ ít dùng bị dọn khi tải thêm. Gói thiếu thành phần sẽ báo cần sửa, không báo sẵn sàng giả.

## Khi mất mạng

Mở lại cùng địa chỉ, cùng trình duyệt và hồ sơ đã tải gói. Có thể tìm địa điểm, đọc giới thiệu/bản dịch, xem ảnh đã lưu, bản đồ vector và nghe MP3 trong gói. Không cần tạo MP3 mới. Có thể tạm dừng hai container để kiểm tra cache của trang; sau kiểm tra chạy lại Compose.

Admin, chủ địa điểm, cập nhật nội dung, dịch văn bản mới và tạo MP3 mới vẫn cần Backend hoặc Internet tương ứng. Offline không bao gồm việc chỉnh sửa dữ liệu quản trị rồi đồng bộ. GPS cần quyền người dùng và phụ thuộc thiết bị; không được coi là đã kiểm chứng chỉ vì bản đồ hiển thị.

## Lưu ý dữ liệu

- Dữ liệu trình duyệt tại cổng 5173 không dùng chung với cổng 8080; máy khác cũng phải tải gói riêng.
- Không xóa dữ liệu website, không dùng chế độ riêng tư, không chọn xóa gói trước buổi demo.
- File âm thanh/bản đồ không đưa vào Git. Chúng nằm trong volume Docker; không dùng `docker compose down -v` nếu muốn giữ lại.
- Gói đầy đủ được kích hoạt sau khi mọi thành phần đã kiểm tra; mất mạng, hủy tải hay checksum sai sẽ giữ gói cũ.
- Khi MP3 đã được nhập lại hoặc tạo lại trên server, luồng tải kiểm chứng bỏ qua HTTP cache để tránh dùng file cũ với manifest mới.
