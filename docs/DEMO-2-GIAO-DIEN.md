# Demo hai giao diện SmartTour Guide

Phạm vi: giao diện khách và giao diện Admin quản lý địa điểm thuyết minh (POI). Giữ ASP.NET Core ba lớp, API thật và tám địa điểm Quận 1. Ngày kiểm tra: 21/10/2026.

## Chạy local — phương án chính cho buổi trình diễn

Terminal thứ nhất:

```powershell
cd C:\CNPM\ShortVideoTranslator
$env:TextToSpeech__PythonArguments__0 = '-3.14'
$env:TextToSpeech__TimeoutSeconds = '180'
dotnet run --project ShortVideoTranslator.Api --launch-profile http
```

Terminal thứ hai:

```powershell
cd C:\CNPM\ShortVideoTranslator\ShortVideoTranslator.Frontend
npm.cmd run dev
```

Giữ cả hai cửa sổ mở. Backend phải hiện `Now listening on: http://localhost:5132`; Vite phải hiện `http://localhost:5173/`. Dùng `npm.cmd` để tránh lỗi PowerShell chặn `npm.ps1`. Khi chuyển máy phải cài phụ thuộc như hướng dẫn SMARTTOUR-DEMO.md và giữ dữ liệu cần thiết trong App_Data riêng; không đẩy dữ liệu/tài khoản lên GitHub.

Docker là lựa chọn bổ sung: xem `DOCKER-DEMO.md`, chưa thay thế cách local đã kiểm tra khi Docker Engine chưa chạy được.

## Trình diễn giao diện khách — khoảng 4 phút

1. Mở http://localhost:5173, chọn Tiếng Việt và bắt đầu khám phá. Khi chưa tải đủ audio ưu tiên vẫn có thể xem địa điểm trước.
2. Giới thiệu giao diện cam–kem, tám thẻ địa điểm có ảnh, tên và địa chỉ. Ảnh có nguồn và ngày ảnh khi biết; không coi ảnh lịch sử là hiện trạng hôm nay.
3. Tìm `nguyen hue` không dấu, chọn Phố đi bộ Nguyễn Huệ. Mở bài giới thiệu đầy đủ, chỉ ra thông tin có nguồn và ảnh tự chuyển ba giây.
4. Nhấn Nghe thuyết minh; dùng Dừng và Phát lại. Chọn English, xem bài dịch và nghe lại. Nguyên bản luôn được gửi đến Backend; không dùng giọng ngoại ngữ đọc tiếng Việt.
5. Chọn địa điểm khác khi đang phát để minh họa dừng audio cũ. Bài đang chọn đã dịch xong có thể nghe ngay, không phụ thuộc việc ba bài ưu tiên khác đã tải hết.
6. Mở bản đồ trang chủ, chọn một địa điểm từ bản đồ để cập nhật chi tiết. Tọa độ là vị trí tham khảo có nguồn, không phải lối vào đã đo GPS thực địa.

## Trình diễn Admin quản lý POI — khoảng 5 phút

1. Mở http://localhost:5173/admin, đăng nhập tài khoản đã có của bạn. Không trình chiếu file mật khẩu hoặc secrets.
2. Chọn **Địa điểm**. Chỉ ra số lượng Tất cả/Công khai/Ẩn/Chờ audio/Lưu trữ, tìm không dấu và lọc danh mục, trạng thái.
3. Chọn **Thêm địa điểm**, nhập một bản ghi có tên rõ là thử nghiệm. Biểu mẫu gồm: thông tin & câu chuyện; địa chỉ & bản đồ; hình ảnh; thuyết minh & xuất bản. Tên, danh mục và bài Việt là bắt buộc. Giữ nhãn demo cho dữ liệu thử và không bịa địa chỉ/tọa độ.
4. Lưu rồi tìm lại bản ghi. Chọn Sửa để đổi nội dung; lưu và tải lại để xác nhận lưu bền qua API.
5. Dùng Công khai/Ẩn. Địa điểm demo có thể xuất hiện ngay; địa điểm chính thức cần nguồn và MP3 tiếng Anh đúng nội dung. Nút **Xem trên website** mở đúng POI đang chọn.
6. Trên bản ghi thử, nhấn Xóa và đọc xác nhận trước khi quyết định. Dùng **Ẩn** nếu chỉ muốn tạm ngừng hiển thị; Xóa không có thao tác hoàn tác trong giao diện.
7. Mở Bản dịch thuyết minh hoặc Tác vụ audio nếu cần chuẩn bị lời đọc theo bài mới. Nội dung nguồn thay đổi không dùng lại MP3 của bài cũ. AI biên tập là tùy chọn và không chặn thao tác CRUD khi chưa cấu hình.

## Kiểm chứng và giới hạn

- `dotnet build --no-restore`: thành công, 0 lỗi/0 cảnh báo; `dotnet test --no-build`: 110/110 đạt.
- `npm.cmd run lint`: sạch; `npm.cmd test`: 39/39 đạt; `npm.cmd run build`: thành công, vẫn còn cảnh báo dung lượng chunk bản đồ.
- Ca HTTP CRUD dùng kho test riêng: thêm, sửa tên/nội dung/địa chỉ, lưu bền, công khai, ẩn, công khai lại, xóa, 404 công khai sau ẩn/xóa và 409 khi cập nhật phiên bản cũ.
- Browser thật: tìm Admin `duc ba`; mở biểu mẫu thêm, chặn tên rỗng và hủy không thêm bản ghi. Trình duyệt từ chối cổng kiểm thử 5174, nên chưa chạy toàn bộ thao tác ghi/xóa qua UI trên bản sao đó. Không thử xóa các địa điểm thật của người dùng.
- Browser thật: Nguyễn Huệ phát Việt 100,656 giây và Anh 92,832 giây; audio không lỗi giải mã, dừng về giây 0 và đổi ngôn ngữ gỡ audio cũ. Tìm `nguyen hue` và nội dung tiếng Anh được kiểm tra trực tiếp.
- MP3 đúng nguồn hiện tại: **48/48** (8 POI × 6 ngôn ngữ) tải được qua API, khớp byte/SHA-256 và ffprobe xác nhận codec MP3, thời lượng dương. Không đồng nghĩa đã nghe hết 48 bản hoặc hiệu đính bản ngữ. Tao Đàn tiếng Việt đã tạo qua POST `/api/narration/speech` thật trong 70,4 giây, audio 98,592 giây; trước đó có các lần timeout. Phiên Backend hiện dùng ngân sách TTS 180 giây, không dùng lại MP3 của nội dung cũ.
- Browser thật: bấm lại tab Địa điểm vẫn tải đủ tám dòng sau khi sửa lỗi đứng ở trạng thái đang tải. Tám ảnh bìa đều tải được; giao diện khách ở chiều rộng 390 px không tràn ngang (vùng nội dung 375 px, hai cột).
- Bản dịch SmartTourDraft là bản nháp AI hỗ trợ, chưa được chuyên gia ngôn ngữ hiệu đính. Edge-TTS là dịch vụ mạng; chuẩn bị MP3 trước buổi demo.
- Không tuyên bố đã kiểm tra GPS đi bộ thực tế, hosting production hoặc CD. Bằng chứng test và ảnh giao diện lưu trong `artifacts/demo-ui-20261021`, không commit.
