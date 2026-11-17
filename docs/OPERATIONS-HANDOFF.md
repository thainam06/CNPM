# Bàn giao kiểm thử và vận hành — 17/11/2026

## Kết quả và giới hạn

Đã kiểm chứng quy trình phát triển và khôi phục trên kho SQLite riêng trong Linux cloud.
Chưa xác nhận hoàn thành toàn bộ điều kiện trong đề: điện thoại/GPS thật, nhiều thiết bị
vật lý và khởi động lại máy chủ cần chạy thực tế. Không thay bằng kết quả mô phỏng.

| Yêu cầu | Bằng chứng lần chạy này | Trạng thái |
|---|---|---|
| Backend build và tests | Release build: 0 warning/error; 141 đạt, 1 PostgreSQL integration test bỏ qua vì chưa cấu hình database ngoài | Đạt với SQLite |
| Frontend | Lint, build và 60 unit tests đạt | Đạt |
| GPS/mobile/audio/offline | 10 browser scenarios trên Chromium; API/GPS/WAV fixture; viewport 320/390/768/1280 | Đạt mô phỏng |
| Nhiều client, mã riêng, OS/browser, lượt xem/nghe | 3 client HTTP đồng thời vào API thật; Android/Chrome, iOS/Safari, Linux/Firefox là nhãn đầu vào tổng hợp; Admin phản ánh đúng từng mã và mỗi loại 1 sự kiện, kể cả gửi lại batch | Đạt client tổng hợp, chưa phải thiết bị thật |
| Rút đồng ý | API forget xóa events, pendingAnalytics, analyticsHeartbeats, analyticsDevices trong SQL; mã cũ bị chặn sau mở lại kho | Đạt |
| Hết active sau 5 phút | Test clock kiểm tra còn online ở 4:59 và offline tại 5:00; lịch sử xem/nghe giữ nguyên | Đạt tự động, chưa chờ 5 phút trên điện thoại |
| Presence rời trang | Heartbeat 3 phiên, leave 1 phiên, Admin còn 2; luồng độc lập analytics | Đạt qua HTTP |
| Backup/restore kho riêng | 6 file đối chiếu SHA-256, 20 bảng đối chiếu số dòng; API thứ hai mở kho khôi phục và đăng nhập đúng mật khẩu cũ, catalog và analytics giữ nguyên; MP3 tone thật giải mã được, không phải giọng đọc | Đạt |
| Chạy lại tiến trình | API nguồn dừng/chạy lại, tài khoản và catalog giữ nguyên | Đạt |
| Docker recreate/persistence | Đã thêm script và job CI; build cloud bị 403 tại `centralus.data.mcr.microsoft.com` khi tải base image | Chưa xác minh runtime Docker |
| Máy chủ reboot | Compose có `restart: unless-stopped`; cần thử reboot trên máy vận hành | Chưa chạy |
| CI commit mới | Workflow bổ sung backup/restore, browser artifact và Docker persistence. GitHub API bị proxy từ chối 403 trong cloud; không kết luận CI xanh từ kết quả local | Kiểm tra trên GitHub sau push |
| CD | Chưa có máy chủ triển khai cố định hoặc CD | Chưa cấu hình |

[Kết quả browser JSON](operations-evidence/browser-results.json),
[ảnh GPS mobile mô phỏng](operations-evidence/gps-nearby-mobile.png),
[ảnh từ chối GPS](operations-evidence/gps-denied.png).
Không có ảnh/backup chứa tài khoản thật trong commit.

## Chạy lại kiểm chứng tự động

Cần .NET 10, Node 24, Python 3.11+ (dùng `hashlib.file_digest`) và FFmpeg.
Trong môi trường cloud đã chuẩn bị, chạy `source /workspace/.cnpm-tools/activate.sh`
trước các lệnh dưới. Máy cá nhân dùng PATH của bộ công cụ đã cài.

```bash
dotnet restore ShortVideoTranslator.slnx
dotnet build ShortVideoTranslator.slnx -c Release --no-restore
dotnet test ShortVideoTranslator.Tests/ShortVideoTranslator.Tests.csproj -c Release --no-build --no-restore
python3 scripts/tests/test_backup_restore.py -v
python3 scripts/verify_operations.py
cd ShortVideoTranslator.Frontend
npm ci
npm run lint
npm test
npx playwright install --with-deps chromium
npm run test:browser
```

`verify_operations.py` khởi động API thật ở cổng tạm, tạo database/tài khoản ngẫu nhiên
trong thư mục tạm, kiểm thử nhiều client, dừng API, backup và chạy API trên bản khôi phục.
Không dùng hoặc xóa database demo đang chạy. Chương trình tự xóa dữ liệu thử khi kết thúc;
không in mật khẩu/token hoặc bản ghi người dùng. Các test chống backup hỏng còn kiểm tra
checksum sai, file thừa, symlink, WAL và từ chối ghi đè kho đích.

Docker, từ thư mục repo trên máy có Docker Engine và Internet:

```bash
bash scripts/verify_docker.sh
```

Script tạo Compose project/volume/cấu hình ngẫu nhiên riêng, build image, đợi healthcheck,
đối chiếu catalog và hash dòng tài khoản trước/sau **tạo lại container**, kiểm tra file
trong volume vẫn còn, rồi xóa đúng project thử. Không chạy `down -v` trên project demo.
Script chưa được chạy hết trong cloud do registry 403; kiểm tra job `docker-persistence`
trên GitHub. Đây không phải bằng chứng reboot hệ điều hành.

## Backup và khôi phục đầy đủ trên kho riêng

`GET /api/admin/backup` chỉ xuất catalog/menu; không thay thế backup database, tài khoản,
audio, keys và cấu hình. `Backup-Sqlite.ps1` chỉ sao lưu SQLite. Với Docker đang vận hành,
dùng `Backup-Demo.ps1` để dừng API sạch và lấy App_Data, keys, `.env.docker` như hướng dẫn
[REUSE-DEPLOYMENT.md](REUSE-DEPLOYMENT.md). Không copy DB đang ghi WAL bằng thao tác copy file đơn lẻ.

Công cụ Python mới dùng được trên Linux/Windows khi đã có các thư mục nguồn trên máy:

```bash
# Dừng TẤT CẢ tiến trình ghi vào App_Data/keys trước lệnh này.
# Các đường dẫn dưới là ví dụ; thay bằng kho thật và vị trí backup riêng của bạn.
python3 scripts/backup_restore.py backup --api-stopped \
  --data /srv/smarttour/App_Data \
  --keys /srv/smarttour/DataProtection-Keys \
  --config /srv/smarttour/.env.docker \
  --destination /srv/private-backups/smarttour-20261117
python3 scripts/backup_restore.py verify --source /srv/private-backups/smarttour-20261117
python3 scripts/backup_restore.py restore \
  --source /srv/private-backups/smarttour-20261117 \
  --destination /srv/smarttour-restore-check
```

`--api-stopped` là xác nhận của người vận hành, không tự dừng server. Đừng dùng cờ này
khi server còn ghi. Kho mới giữ `App_Data`, `DataProtection-Keys`, `.env.docker` và manifest.
Đích phải chưa tồn tại, không nằm trong nguồn; tool không ghi đè kho hiện hành. Bản copy
SQLite được gộp WAL bằng SQLite backup API, kiểm tra integrity, số dòng bảng và SHA-256 từng
file; restore xác minh cả trước và sau copy. SHA-256 phát hiện hỏng dữ liệu, không chứng minh
nguồn gốc backup nếu kẻ khác thay được cả manifest. Giữ bản backup trong nơi riêng, quyền hạn
chế, mã hóa khi lưu/chuyển bằng công cụ quản trị phù hợp; không commit/push backup hoặc secrets.

Sau restore, mở API thử với `Storage__Directory` trỏ vào `App_Data` vừa khôi phục, cổng khác
và môi trường Development nếu chỉ thử local; không kết nối dữ liệu production. Đối chiếu
readiness, đăng nhập tài khoản cũ, POI, lượt xem/nghe và MP3 trước khi lên kế hoạch chuyển kho.
Với Docker, phải gắn đúng volume keys và đặt lại owner `app:app` cho hai thư mục như hướng dẫn
vận hành. Không trỏ hai API worker vào cùng một kho đang vận hành. Giữ backup cũ để rollback.

## Kiểm tra thực tế còn phải thực hiện

1. Dùng link HTTPS trên ít nhất một điện thoại. Ghi ngày giờ, model/OS/browser, ngôn ngữ,
   khu vực thử và độ chính xác GPS; tránh đưa tọa độ riêng tư vào báo cáo công khai.
2. Cho phép GPS, đi bộ giữa các POI đã xác minh: thứ tự/khoảng cách đổi, không phát lặp;
   từ chối quyền, tắt GPS, bật lại và kiểm tra trạng thái không giữ vị trí cũ.
3. Mở đồng thời ít nhất hai trình duyệt/thiết bị thật, đồng ý analytics, tạo lượt xem/nghe;
   Admin thấy mã khác nhau và nhãn đúng. Không gọi số mã là số thiết bị vật lý tuyệt đối.
4. Đóng một trình duyệt; bảng analytics hết online sau hơn 5 phút. Bảng presence độc lập
   thường mất ngay khi leave, hoặc sau 45 giây nếu mất tín hiệu. Rút đồng ý và xác nhận dữ liệu
   analytics biến mất, không yêu cầu presence vận hành tự biến mất theo luồng analytics.
5. Nghe nội dung đúng POI/ngôn ngữ, tải gói offline trước khi mất mạng rồi reload và nghe lại.
   Tiếng vi/en mới kiểm thử luồng bằng fixture; phát âm thực tế và zh/ja/ko/fr chưa được xác nhận.
6. Trên máy vận hành, backup trước, reboot máy/Docker Engine, đợi container healthy rồi đối
   chiếu tài khoản/POI/MP3/SQL. Chỉ ghi đạt sau khi thực hiện; không reset tài khoản/khởi tạo lại kho.

Mẫu ghi kết quả (để trống cho tới khi có thiết bị thật):

| Ngày giờ | Thiết bị/OS/browser | Ngôn ngữ | GPS đi bộ/quyền | Xem/nghe & Admin | Offline | Bằng chứng | Kết quả |
|---|---|---|---|---|---|---|---|
| Chưa thử | Android | vi/en/zh/ja/ko/fr | Chưa thử | Chưa thử | Chưa thử | Chưa có | Chờ |
| Chưa thử | iPhone | vi/en/zh/ja/ko/fr | Chưa thử | Chưa thử | Chưa thử | Chưa có | Chờ |

## Lỗi thường gặp và đường dẫn CI

- `.NET` báo mutex `NuGet-Migrations` trong sandbox: dùng cơ chế quyền runtime được hỗ trợ;
  không vô hiệu hóa bảo mật hoặc sửa dependency để lách lỗi.
- npm cache không ghi được: dùng `npm_config_cache` trỏ vào thư mục có quyền ghi; giữ `npm ci`
  và integrity của lockfile.
- Registry Microsoft trả 403: cho phép `mcr.microsoft.com` và hostname CDN cụ thể báo trong
  lỗi (lần này `centralus.data.mcr.microsoft.com`) qua quản trị mạng, rồi build lại. Không tắt TLS.
- Không có GPS: kiểm tra HTTPS, quyền site, định vị OS và mẫu mới; không ép cấp quyền bằng code.
- MP3/translation không tạo được: kiểm tra Python Edge-TTS đúng version, FFmpeg và truy cập
  provider; không coi audio fixture là chứng minh provider đang hoạt động.
- Restore báo checksum/inventory sai: giữ nguyên backup để điều tra, lấy lại bản đầy đủ tin cậy;
  không sửa manifest để bỏ qua lỗi.
- CI: [workflow](https://github.com/thainam06/CNPM/actions/workflows/dotnet-ci.yml),
  [runs nhánh bàn giao](https://github.com/thainam06/CNPM/actions?query=branch%3Afeature%2Foperations-verification).
  Xem đúng SHA commit đã push, hai job build/test và Docker; artifact browser giữ 7 ngày.
  Nếu dùng nhánh để review, tạo PR về `main` sau khi xem diff; không suy ra CI thành công khi chưa đọc kết quả.
