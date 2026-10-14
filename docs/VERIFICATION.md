# Kết quả kiểm tra SmartTour Guide — cập nhật 14/10/2026

## Kết quả hiện tại — chuyển hoàn toàn sang Quận 1

- `dotnet build`: 0 warning, 0 error. `dotnet test --no-build`: 67/67 đạt. Thêm regression test chuyển khu vực giữ nội dung thành viên/tài khoản, cấp ID mới và không tạo trùng sau restart.
- Frontend: `npm run lint`, `npm test` (12/12), `npm run build` đạt; còn cảnh báo chunk MapLibre lớn đã import động.
- API thật: danh sách công khai chỉ gồm Chợ Bến Thành (ID4), Dinh Độc Lập (ID5), Bưu điện Trung tâm Sài Gòn (ID6). `/api/places/1` trả404. Admin vẫn giữ ba bản ghi Quận4, nhãn lưu trữ/ngoài phạm vi; nội dung, tài khoản và thực đơn không bị xóa. Kho mới hoàn toàn dùng IDs1–3, không phụ thuộc ID cố định ở frontend.
- Admin đăng nhập thực tế HTTP200 với tài khoản demo hiện có; giữ nguyên quyền và mật khẩu đã được người dùng yêu cầu.
- Ba tác vụ mới đều completed6/6. Manifest công khai có3MP3 mỗi ngôn ngữ, đã tải và đối chiếu SHA256 toàn bộ18MP3: vi118800/en139680/zh142128/ja148032/ko126720/fr147024 byte. Đây là MyMemory+Edge-TTS thật, không phải fixture.
- Trình duyệt: ảnh thật ba nơi tải đủ; tên địa điểm hiện trên ảnh chi tiết, địa chỉ/nguồn có dẫn. Tìm không dấu `dinh doc lap` trả1; `Bến Nhà Rồng` trả0. MP3 Chợ Bến Thành: Việt duration6.192s; Anh7.584s; readyState4, pausedfalse, errornull khi phát. Không kiểm duyệt chuyên môn bản dịch máy.
- Bản đồ: PMTiles thật trích từ Protomaps20261013,30tileentries,1914259byte,zoom0–15,CLIverify đạt, SHA256 `f7eae506d4fb67027ef8fe9a6ed4435d67f909308efecdc1aba2dad502b7fef9`. `/api/maps/pack-manifest`200scope district1; Range0–126 trả206/127byte.
- Bounds[106.6853694,10.7625222,106.71,10.79] là khung xem quanh ba tọa độ đã dẫn, padding0.01độ; không phải ranh giới hành chính. Tọa độ tham khảo chuyển từ DMS trên Commons, không phải cổng vào đo GPS.
- Kiểm tra tải gói Quận1 → chọnOffline → tắt cả API5132 và Vite5173 → reload: danh sách, ảnh đã xem, MapLibre vector/ba ghim và MP3Vi vẫn mở/phát; không tràn ngang ở viewport hiện tại. Không tuyên bố đã ngắt Internet toàn máy; iframe chi tiết vẫn cần Internet.
- Catalog, trạng thái đồng bộ, gói map và lựa chọn map có khóa district1 riêng; không dùng lại cache Quận4.
- Minh chứng: `artifacts/district1-demo-tested.png`, `artifacts/district1-offline-tested.png` (ignored). Nguồn dữ liệu: [DISTRICT1-SOURCES.md](DISTRICT1-SOURCES.md).

## Giai đoạn Quận 4 trước khi chuyển phạm vi (14/10/2026)

- `dotnet build`: thành công, 0 warning/0 error sau khi bổ sung publish gate, content sync, localization và maintenance.
- `dotnet test --no-build`: 65 test, kiểm tra thêm English readiness đúng hash, không tin trạng thái audio do client gửi, ID không tái sử dụng, delta xóa/ẩn, ETag 304, UI bundle fallback; cùng các kiểm thử auth/CSRF/Bearer/refresh rotation/owner scope/concurrent edits trước đó.
- `npm run lint`, `npm test`, `npm run build`: đạt (build có cảnh báo chunk MapLibre lớn, được lazy load); 12 bài test GPS, hotset sort/radius/limit và LRU 3 ngôn ngữ ×300 MP3.
- `git diff --check`: đạt. Nhánh `feature/api`; không commit/push/merge.
- HTTP thực tế: health ready, content 200 với ETag, map offline readiness true sau khi tạo và verify gói Quận 4. Gói tiếng Việt có 3 MP3, tổng 235.296 byte.
- Trình duyệt production: nút tải gói xác nhận **Đã lưu 3 MP3 và dữ liệu địa điểm cho vi**. Tắt cả Vite preview và API rồi reload: trang vẫn mở nhờ Service Worker, dùng bản dịch đã lưu và phát MP3. Media DOM: duration=13.632, readyState=4, paused=false, error=null. Đây là kiểm tra khi hai server local tắt; không mô phỏng ngắt toàn bộ Internet.
- MapLibre/PMTiles đã kiểm tra reload/zoom khi hai server local tắt. Sửa lỗi Web Worker của MapLibre6 và lỗi module offline do Vary: Origin; shell version/cache hiện gắn build-ID. Gói bản đồ 2.018.245 byte, checksum đã verify. Iframe chi tiết vẫn online; chưa thử ngắt Internet toàn máy. AI thiếu khóa/model: chưa gọi thật, tests HTTP giả lập xác minh quota10/ngày, requiresReview và provider503.
- Tests mới: Range/checksum/path/version map; consent/rate/rounding/revocation/quyền runtime; encryption/tamper/180-day expiry/mask/IDOR giấy tờ; AI/provider/quota; audio control và restart qua cache.
- Ảnh minh chứng bản đồ offline: `artifacts/map-offline-tested.png`.
- Ảnh minh chứng giao diện cam/kem: `artifacts/smarttour-orange-offline.png` (ignored).

Đối chiếu đầy đủ các yêu cầu HTML và phần còn thiếu: [IMPLEMENTATION-STATUS.md](IMPLEMENTATION-STATUS.md). Hướng dẫn chạy/đăng nhập: [SMARTTOUR-DEMO.md](SMARTTOUR-DEMO.md).

## Lịch sử kiểm tra giai đoạn đầu (13/10/2026)

Phần bên dưới là kết quả trước khi thêm admin, ảnh, địa chỉ, map và offline; không dùng danh sách phạm vi cũ làm trạng thái hiện tại.

Thực hiện ngày 13/10/2026 trên Windows, .NET SDK 10.0.401, Node 24.21.0, Python 3.14.2, edge-tts 7.2.8. Đây là kết quả tại môi trường local; GitHub Actions chưa được chạy trên GitHub vì chưa commit/push.

## Build và unit test

| Lệnh | Thư mục | Kết quả |
| --- | --- | --- |
| `dotnet build` | Gốc solution | Đạt, 0 warning, 0 error |
| `dotnet test --no-build` | Gốc solution | 30/30 đạt |
| `npm run build` | ShortVideoTranslator.Frontend | Đạt |
| `npm run lint` | ShortVideoTranslator.Frontend | Đạt, không warning |
| `git diff --check` | Gốc solution | Đạt |

Unit test kiểm tra: dữ liệu và chi tiết địa điểm; nội dung demo không chứa địa chỉ/tọa độ chưa xác minh; bản gốc tiếng Việt không gọi dịch vụ dịch; năm ngôn ngữ đích; cache; hạn mức; bản dịch rỗng/giữ nguyên nguồn/JSON lỗi; tắt provider; timeout; mất mạng; hủy; giới hạn độ dài; chia UTF-8 giữ Unicode; hủy TTS trước khi chạy Python. Ba test SpeechSegment cũ vẫn được giữ.

## HTTP và dịch/TTS trực tuyến

- API local cổng 5132 và Vite cổng 5173 hoạt động.
- Danh sách, chi tiết địa điểm và proxy `/api/places`: HTTP 200; ID 999: 404.
- CORS preflight từ `http://localhost:5173`: đúng `Access-Control-Allow-Origin`, method và header.
- Speech nội dung rỗng, ngôn ngữ `de`, trên 3000 ký tự: HTTP 400 với thông báo.
- Dịch thật qua MyMemory sang en/zh/ja/ko/fr: HTTP 200 và nội dung ngôn ngữ đích. Không đánh giá chuyên môn độ chính xác ngữ nghĩa; tên riêng cần rà soát.
- Speech Việt: HTTP 200 `audio/mpeg`, câu ngắn trả 11.232 byte; nội dung demo Bến Nhà Rồng đã tải/phát trong browser, thời lượng 13,632 giây.
- Speech tiếng Anh cho nội dung demo Bến Nhà Rồng: đã tải/phát trong browser, thời lượng 13,752 giây.
- Với câu kiểm tra “Bạn có thể chọn ngôn ngữ và nghe thuyết minh tự động.”, API dịch và tạo MP3 thật: zh 20.592 byte / 3,432 giây; ja 32.400 byte / 5,400 giây; ko 28.800 byte / 4,800 giây; fr 27.504 byte / 4,584 giây. Thời lượng xác nhận bằng ffprobe. Chưa nghe/kiểm duyệt ngữ nghĩa từng giọng ngoại ngữ.
- Một số lần Edge-TTS trả `NoAudioReceived`; đã bổ sung retry một lần, kiểm tra lại tạo/phát thành công. Dịch vụ trực tuyến vẫn có thể lỗi hoặc hết hạn mức.

## Trình duyệt

- Tải danh sách/chi tiết qua API; chọn địa điểm; hiển thị bản dịch Anh.
- Tìm `vinh khanh` trả một địa điểm, xóa tìm kiếm trả ba.
- Phát lại từ đầu: media `paused=false`, thời gian về 0. Dừng: `paused=true`, thời gian về 0.
- Đổi ngôn ngữ xóa MP3 cũ và chờ bản dịch; đổi địa điểm khi TTS đang tải hủy request, không giữ trình phát cũ.
- Dừng backend và reload: hiện lỗi HTTP 502 qua proxy cùng nút Thử lại. Khởi động backend và nhấn Thử lại: danh sách và chi tiết được phục hồi.
- Viewport 390×844 và 1280×900: không tràn ngang; cột xếp dọc ở mobile và hai cột ở desktop. Đã reset viewport sau kiểm tra.
- Ảnh toàn trang: `artifacts/smarttour-demo.png` (thư mục artifacts được Git ignore).

## File đã chỉnh/thêm

- Frontend: `src/App.jsx`, `src/App.css`, `src/api.js`, `vite.config.js`, `index.html`, `.env.example`, `README.md`. `src/index.css` được kiểm tra và giữ nguyên vì không có lỗi cần sửa.
- API: `Program.cs`, `appsettings.json`, `Controllers/NarrationController.cs`, `Controllers/PlacesController.cs`.
- Business: `TextToSpeechService.cs`, `TranslationService.cs`, `NarrationLanguages.cs`, `Place.cs` (model, interface repository, service).
- Data: `DemoPlaceRepository.cs`.
- Tests: `TranslationServiceTests.cs`, `PlaceServiceTests.cs`, project reference Data trong `.csproj`.
- CI: `.github/workflows/dotnet-ci.yml` bổ sung Node, npm ci, lint và build frontend.
- Tài liệu: `docs/SMARTTOUR-DEMO.md`, `docs/VERIFICATION.md`.

Giữ nguyên các API video, Whisper/FFmpeg, Solution và namespace. Không commit, push hoặc merge. Đã xác nhận Git ignore models, node_modules, dist, bin, obj, MP3 và `.env`; không phát hiện các loại file này trong danh sách file được Git track.

## Còn thiếu để sử dụng chính thức

Nội dung giới thiệu lịch sử/du lịch có nguồn kiểm chứng, ảnh địa điểm thật, địa chỉ/tọa độ chính xác và kiểm duyệt bản dịch. Dữ liệu hiện là demo phục vụ chức năng; bản đồ thông báo chưa sẵn sàng. Chưa kiểm tra hosting production hoặc chạy workflow trên GitHub.

## Kiểm tra trực tuyến cuối ngày 14/10/2026

- `py -3.14 -m pip install --upgrade edge-tts==7.3.1`: thành công; khắc phục403 ở7.2.8.
- Nguồn probe mới45 ký tự: vi 200/audio-mpeg/22.320 byte, en 200/audio-mpeg/14.976 byte.
- Hàng đợi Admin3 địa điểm×6 ngôn ngữ: completed 6/6 tất cả; dữ liệu/audio nguồn demo có nhãn, không dùng bản dịch giả. Giữ lịch sử lỗi cũ.
- Browser playback tiếng Anh:13.752s,readyState4,paused=false,error=null. Browser hotset tự chuẩn bị Hàn:3/3 +bundleUI sẵn sàng.
- Thống kê vị trí Admin cửa sổ24h hiện0 phiên thật; tests dùng tọa độ giả lập. Identity tests dùng số synthetic, không nhập giấy tờ thật.
- Backend 65/65, frontend 12/12,lint sạch,build đều thành công; frontend còn cảnh báo chunk MapLibre lớn. Regression SW VaryOrigin đã được đưa vào test.
- Ảnh `artifacts/admin-audio-six-languages.png`, `artifacts/narration-english-tested.png`, `artifacts/admin-runtime-tested.png`, `artifacts/map-offline-tested.png`.
