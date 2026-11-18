# SmartTour Guide — Quận 1

Website khám phá địa điểm và thuyết minh đa ngôn ngữ. React + Vite gọi ASP.NET Core 10; nghiệp vụ nằm trong Business, lưu trữ trong Data. Mặc định dùng SQLite; dữ liệu JSON cũ được nhập một lần khi chưa có database.

## Cấu trúc thư mục

| Thư mục | Nội dung |
|---|---|
| `ShortVideoTranslator.Api` | Controller, bảo mật, tác vụ nền và cấu hình Backend |
| `ShortVideoTranslator.Business` | Nghiệp vụ, model và hợp đồng kho dữ liệu |
| `ShortVideoTranslator.Data` | SQLite/PostgreSQL và nội dung thuyết minh mẫu |
| `ShortVideoTranslator.Frontend/src/pages` | Trang khám phá và quản trị |
| `ShortVideoTranslator.Frontend/src/components` | Thành phần giao diện dùng lại |
| `ShortVideoTranslator.Frontend/src/services` | API, lưu trữ, theo dõi và chuẩn bị audio |
| `ShortVideoTranslator.Frontend/src/utils` | Hàm xử lý khoảng cách, bộ lọc, mã thiết bị và chính sách |
| `ShortVideoTranslator.Frontend/src/hooks` | React Hooks |
| `ShortVideoTranslator.Frontend/src/data` | Ngôn ngữ giao diện, từ điển và nguồn ảnh |
| `ShortVideoTranslator.Frontend/src/styles` | CSS các trang và thành phần |
| `ShortVideoTranslator.Tests`, `ShortVideoTranslator.Frontend/tests` | Kiểm thử Backend và Frontend |
| `database`, `docker`, `scripts`, `docs` | SQL, triển khai, công cụ vận hành và tài liệu |

VS Code ẩn các thư mục sinh tự động `bin`, `obj`, `node_modules`, `dist`, `.vs` và
`artifacts` để dễ đọc source. Đây là thiết lập hiển thị, không xóa dữ liệu.
Các file JSON cấu hình, thư viện và nội dung đang dùng cần được giữ; không phải
database chỉ vì có đuôi `.json`. Không xóa `App_Data` hoặc Docker volume để dọn source.

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
- Sau khi được trình duyệt cấp quyền GPS, vị trí làm tròn được chia sẻ với Admin, lưu tối đa 24 giờ. Có thể dừng định vị ngay tại phần chọn địa điểm. Thống kê mặc định bật cho lượt truy cập mới và có thể tắt ở cuối trang; lựa chọn từ chối trước đó được giữ.
- Admin: `/admin` → **Tổng quan** → **Thiết bị đang truy cập website** chỉ hiện các trình duyệt đang hoạt động, tự giảm khi rời/ẩn trang; mất tín hiệu tự hết hạn sau 45 giây. Không hiện bộ đếm ở trang khách. Bảng lượt xem/nghe có đồng ý được giữ riêng.
- Offline: nghe MP3 đã được chuẩn bị/lưu trên thiết bị; gói bản đồ có thể tải từ khu vực bản đồ. Các tài liệu kiểm thử offline trước đây nằm tại [OFFLINE-DEMO.md](docs/OFFLINE-DEMO.md).

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

## Kiểm chứng vận hành và bàn giao

Xem [báo cáo, công cụ backup/restore và checklist thiết bị thật](docs/OPERATIONS-HANDOFF.md). Báo cáo phân biệt kiểm thử tự động, GPS mô phỏng và các bước thực tế chưa xác minh.

## Dữ liệu Supabase và bàn giao nhóm

Schema dùng `smarttour_demo.users`, `places`, `place_images`, `place_narrations`,
`visitor_sessions`, `activity_logs`. Xem [bộ SQL và hướng dẫn kiểm tra chỉ đọc](database/supabase-review/README.md)
và [cách mở dữ liệu/chạy web](HUONG-DAN-XEM-SUPABASE.md).
