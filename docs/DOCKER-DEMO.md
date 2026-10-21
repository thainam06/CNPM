# Chạy SmartTour Guide bằng Docker

## Trạng thái kiểm tra ngày 21/10/2026

Đã build và chạy thực tế bằng Docker Desktop sau khi hoàn tất WSL. Hai container `api` và `web` đều healthy; `http://localhost:8080/`, `/admin`, `/api/health/ready` và `/api/places` trả HTTP 200. API trả 8 địa điểm. Đăng nhập tài khoản bootstrap thành công và yêu cầu đổi mật khẩu lần đầu. API dịch nội dung địa điểm sang tiếng Anh trả văn bản khác tiếng Việt (1.324 ký tự). Edge-TTS trong container tạo mẫu tiếng Việt thành công: HTTP 200, audio/mpeg, 18.432 byte. Chưa kiểm thử toàn bộ 48 bài MP3 hoặc thao tác giao diện trình duyệt trên bản Docker; kết quả 48 MP3 trước đó thuộc bản chạy trực tiếp trên Windows.

## 1. Chuẩn bị máy Windows

1. Docker Desktop đã cài xong. Lưu công việc rồi tự khởi động lại Windows để hoàn tất hai tính năng ảo hóa; không cần chạy lại bộ cài Docker.
2. Nếu `wsl --status` vẫn báo chưa cài sau khi khởi động lại, mở **Terminal → Run as administrator**, chạy `winget install --id Microsoft.WSL --exact --source winget` hoặc làm theo thông báo cài WSL của Docker Desktop. Chấp thuận UAC nếu bạn đồng ý cài và làm theo yêu cầu khởi động lại nếu có. Không tắt máy giữa lúc Windows đang cài tính năng.
3. Mở **Docker Desktop** từ Start Menu. Chính bạn đọc/chấp nhận điều khoản nếu ứng dụng yêu cầu. Dùng Linux containers và WSL 2. Chờ Engine running.
4. Mở Terminal mới và kiểm tra:

```powershell
docker version
docker compose version
wsl --status
```

`docker version` cần hiện cả Client và Server. Nếu chỉ có Client và lỗi kết nối pipe, Engine chưa sẵn sàng. Nếu CLI chưa có trong PATH, đóng/mở Terminal hoặc kiểm tra thư mục cài Docker Desktop.

Nguồn cài chính thức: https://docs.docker.com/desktop/setup/install/windows-install/

## 2. Cấu hình tài khoản Docker riêng

Tại `C:\CNPM\ShortVideoTranslator` đã có **`.env.docker`** được tạo ngẫu nhiên và Git ignore. Mở file này trên máy để xem `SMARTTOUR_ADMIN_USERNAME` và `SMARTTOUR_ADMIN_PASSWORD`. Không gửi file này lên GitHub hoặc chụp ảnh để nộp báo cáo.

- Tài khoản Docker mới độc lập với tài khoản chạy local tại cổng 5132.
- Khi tạo kho mới, lần đăng nhập đầu tiên sẽ yêu cầu đổi mật khẩu.
- Đổi `SMARTTOUR_ADMIN_PASSWORD` sau khi kho đã có tài khoản không tự đặt lại mật khẩu tài khoản đó.
- `SMARTTOUR_PORT` mặc định là 8080; đổi nếu trùng cổng.
- `SMARTTOUR_JWT_SECRET` phải là chuỗi ngẫu nhiên ít nhất 32 ký tự.

Trên máy thành viên chưa có `.env.docker`, có thể chạy helper nếu PowerShell cho phép script local:

```powershell
.\scripts\Docker-Demo.ps1 -Action Init
```

Hoặc sao chép `.env.docker.example` thành `.env.docker`, tự thay mật khẩu và secret mẫu trước khi chạy. Không cần thay đổi chính sách PowerShell để dùng các lệnh Docker bên dưới.

## 3. Chạy

```powershell
cd C:\CNPM\ShortVideoTranslator
docker compose --env-file .env.docker config --quiet
docker compose --env-file .env.docker up --build -d --wait --wait-timeout 240
docker compose --env-file .env.docker ps
```

Lần đầu tải base image, NuGet, npm, Ubuntu packages và Python packages nên cần Internet và có thể mất nhiều phút. Image không chứa `node_modules` của máy Windows, `bin/obj`, `App_Data`, model Whisper hoặc thông tin bí mật.

Mở:

- Khách: http://localhost:8080/
- Admin: http://localhost:8080/admin
- Kiểm tra API: http://localhost:8080/api/health/ready

Nếu đổi `SMARTTOUR_PORT`, thay 8080 tương ứng. Nginx chuyển `/api` đến Backend qua mạng nội bộ Compose, nên trình duyệt chỉ dùng một origin và không phải cấu hình API URL riêng.

## 4. Xem lỗi, dừng và mở lại

```powershell
docker compose --env-file .env.docker logs --tail 100 api web
docker compose --env-file .env.docker down
docker compose --env-file .env.docker up -d --wait
```

`down` trong hướng dẫn này giữ volume. Không thêm tùy chọn `-v` nếu muốn giữ tài khoản, địa điểm, ảnh và MP3. Nếu code thay đổi, dùng lại `up --build -d --wait`.

## 5. Dữ liệu và giới hạn

- Volume `smarttour-data` chứa kho JSON, media, MP3 và gói bản đồ nếu được chuẩn bị. Volume `smarttour-keys` giữ khóa bảo vệ phiên đăng nhập.
- Kho Docker mới có tám địa điểm mẫu từ source; **không tự sao chép** ảnh tải lên, bài đã chỉnh, tài khoản và MP3 của `ShortVideoTranslator.Api/App_Data` trên Windows. Muốn dùng đúng dữ liệu hiện tại phải bàn giao/khôi phục dữ liệu riêng, không commit vào Git.
- Trong Admin → Tác vụ audio, chọn địa điểm và ngôn ngữ để chuẩn bị MP3 trước buổi diễn. MP3 tạo mới vẫn cần mạng đến dịch vụ Edge-TTS. Không tạo bản dịch giả nếu dịch vụ thiếu hoặc lỗi.
- Bản đồ online và ảnh remote cần Internet. Bản đồ offline cần chuẩn bị gói dữ liệu phù hợp. GPS/PWA trên thiết bị truy cập qua mạng cần môi trường bảo mật thích hợp.
- Chức năng video/Whisper cũ cần model riêng; model không được nhúng vào image. Bộ Docker này ưu tiên demo du lịch, CRUD POI và Edge-TTS.
- Compose này chỉ bind cổng vào **127.0.0.1**, chạy môi trường Development cho demo HTTP trên máy. Chưa phải cấu hình công khai Internet. Đưa lên hosting cần HTTPS, môi trường Production, secrets và cấu hình proxy tương ứng.
- Máy người khác có Docker Engine/Compose phù hợp có thể build cùng source, nhưng mỗi máy có volume riêng. Điện thoại dùng trình duyệt để truy cập máy chủ, không cài Docker để chạy website.

## 6. Dự phòng cho buổi demo

Khi Docker/WSL chưa hoàn tất, dùng cách local đã kiểm tra trong `DEMO-2-GIAO-DIEN.md`. Không để phần cài Docker ngăn demo hai giao diện chính. Đóng gói Docker không đồng nghĩa đã có CD tự triển khai lên hosting.
