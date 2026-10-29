# SmartTour Guide — Quận 1

Website khám phá địa điểm và thuyết minh đa ngôn ngữ. React + Vite gọi ASP.NET Core 10; nghiệp vụ nằm trong Business, lưu trữ trong Data. Mặc định dùng SQLite; dữ liệu JSON cũ được nhập một lần khi chưa có database.

## Chạy bằng Docker

Cài Docker Desktop, mở ứng dụng và chờ Engine running. Trong PowerShell:

```powershell
cd C:\CNPM\ShortVideoTranslator
# Chỉ chạy Init nếu chưa có .env.docker. Không ghi đè file đang dùng.
& ./scripts/Docker-Demo.ps1 -Action Init
& ./scripts/Start-PublicDemo.ps1 -Build
```

Local: http://localhost:8080. Link HTTPS công khai nằm trong log `https://…trycloudflare.com`. Chia sẻ link này cho điện thoại hoặc máy khác có Internet. Máy chủ phải bật, có mạng và Docker phải chạy. Khởi động lại tunnel có thể đổi link; xem cách dùng tên miền cố định trong [hướng dẫn vận hành](docs/REUSE-DEPLOYMENT.md).

Những lần sau: `& ./scripts/Start-PublicDemo.ps1`. Khi sửa source, thêm `-Build`. Xem trạng thái: thêm `-Action Status`; dừng: thêm `-Action Stop`.

## Chức năng sử dụng

- Khách: chọn địa điểm, đọc giới thiệu, chọn ngôn ngữ và nghe MP3 Edge-TTS.
- Mở trang, hệ thống tự yêu cầu vị trí và gợi ý địa điểm gần. Lần đầu chọn **Cho phép** trong thông báo của trình duyệt. Khi di chuyển, danh sách tự cập nhật và xếp theo khoảng cách đường thẳng trong bán kính đã chọn. Chỉ dùng tọa độ địa điểm đã xác minh.
- Gợi ý GPS chỉ xử lý trên máy khách. Chia sẻ vị trí với Admin là lựa chọn riêng trong Tiện ích.
- Admin: `/admin` → **Tổng quan** → **Thiết bị đang truy cập website** chỉ hiện các trình duyệt đang hoạt động, tự giảm khi rời/ẩn trang; mất tín hiệu tự hết hạn sau 45 giây. Không hiện bộ đếm ở trang khách. Bảng lượt xem/nghe có đồng ý được giữ riêng.
- Offline: tải gói trước khi mất mạng; hướng dẫn tại [OFFLINE-DEMO.md](docs/OFFLINE-DEMO.md).

Tài khoản của bản đang chạy được giữ trong database. Không khởi tạo lại dữ liệu hoặc đổi mật khẩu khi chỉ cập nhật code. `.env.docker` chứa cấu hình khởi tạo riêng, không commit.

## Phát triển và kiểm thử

Cần .NET 10, Node.js 24 và Python có Edge-TTS nếu tạo MP3 ngoài Docker.

```powershell
dotnet build
dotnet test
cd ShortVideoTranslator.Frontend
npm.cmd ci
npm.cmd run lint
npm.cmd test
npm.cmd run build
```

Chạy local bằng hai terminal: `dotnet run --project ShortVideoTranslator.Api --launch-profile http` và `npm.cmd run dev` trong Frontend. API ở 5132; Vite ở 5173. Dữ liệu local và Docker là hai kho riêng. Điện thoại nên dùng link Cloudflare HTTPS để có định vị.

Chi tiết kiến trúc, mở rộng, SQL, sao lưu và chuyển máy: [REUSE-DEPLOYMENT.md](docs/REUSE-DEPLOYMENT.md).
