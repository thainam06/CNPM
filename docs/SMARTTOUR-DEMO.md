# SmartTour Guide — hướng dẫn demo

Giữ nguyên Solution và namespace ShortVideoTranslator. Các API video/Whisper cũ vẫn còn nguyên. Nhánh làm việc: `feature/api`; không tự commit, push hoặc merge.

## Chạy trên Windows

Yêu cầu: .NET SDK10, Node.js24, Python 3.14 với edge-tts 7.3.1 đã kiểm thử và kết nối Internet. Cài bằng `py -3.14 -m pip install -r ShortVideoTranslator.Api/requirements-tts.txt`; không gọi script edge-tts qua PATH vì máy này dùng `py -m edge_tts`.

Từ thư mục gốc:

```powershell
dotnet run --project ShortVideoTranslator.Api --launch-profile http
```

Trong terminal thứ hai:

```powershell
cd ShortVideoTranslator.Frontend
npm ci
npm run dev
```

Mở http://localhost:5173. Backend: http://localhost:5132. Vite proxy `/api` sang backend. `API_PROXY_TARGET` thay đổi đích proxy; `VITE_API_BASE_URL` đặt URL backend công khai khi build frontend cho deployment. Không đặt khóa bí mật vào biến `VITE_*`. Khi deploy dùng cùng origin với reverse proxy `/api`, hoặc cấu hình `Cors:AllowedOrigins` phù hợp.

## Các endpoint

| Method | Route | Hành vi |
| --- | --- | --- |
| GET | `/api/places` | Danh sách ba địa điểm demo |
| GET | `/api/places/{id}` | Chi tiết; 404 khi không tồn tại |
| POST | `/api/narration/translate` | Nhận `{ "text": "nội dung tiếng Việt", "language": "en" }`; trả `text`, `language`, `provider`, `isMachineTranslation` |
| POST | `/api/narration/speech` | Cùng request; backend dịch nguồn tiếng Việt rồi tạo MP3 với giọng đúng ngôn ngữ |

Ngôn ngữ: `vi`, `en`, `zh`, `ja`, `ko`, `fr`. Nguồn đầu vào luôn là tiếng Việt. Không gửi bản dịch trở lại `/speech`, tránh dịch hai lần.

## Dịch và giọng đọc

- Giọng Việt: `vi-VN-HoaiMyNeural`. Không dùng `speechSynthesis`.
- MyMemory là API miễn phí không cần Azure, thẻ ngân hàng hoặc API key. Nội dung được gửi tới MyMemory; nội dung đã dịch được gửi tới dịch vụ Edge TTS trực tuyến. Cần Internet.
- Theo [tài liệu giới hạn MyMemory](https://mymemory.translated.net/doc/usagelimits.php), truy cập ẩn danh giới hạn 5.000 ký tự/ngày. [API specification](https://mymemory.translated.net/doc/spec.php) giới hạn 500 byte UTF-8 mỗi truy vấn; service chia đoạn tối đa 480 byte, giữ Unicode và ưu tiên ranh giới từ.
- Bản dịch máy có thể sai tên riêng, ngữ nghĩa; cần người kiểm duyệt trước khi dùng làm tư liệu du lịch. Không coi bản dịch là nguồn thông tin lịch sử.
- Cache tối đa 128 bản dịch trong bộ nhớ một phiên backend giúp giảm gọi dịch khi nghe lại. Bản dịch đi kèm MP3 được lưu bền trong App_Data/audio; cache dịch độc lập vẫn ở bộ nhớ. Hết hạn mức/lỗi/mất mạng sẽ trả 503, khóa nghe và cho thử lại; không tạo bản dịch giả hoặc đọc tiếng Việt bằng giọng ngoại ngữ.
- `Translation:Enabled=false` tắt dịch ngoại ngữ; tiếng Việt vẫn dùng được. Timeout dịch mặc định 25 giây.
- [edge-tts](https://github.com/rany2/edge-tts) sử dụng dịch vụ trực tuyến của Microsoft Edge, không có bảo đảm ổn định như dịch vụ thương mại. Lỗi `NoAudioReceived` được thử lại một lần trong timeout tổng 90 giây.
- `TextToSpeech:PythonExecutable` mặc định `py` trên Windows, `python3` trên Linux. Có thể đặt đường dẫn Python của môi trường ảo bằng cấu hình hoặc biến môi trường `TextToSpeech__PythonExecutable`. `PythonArguments` hỗ trợ chọn phiên bản, ví dụ `["-3.14"]` với `py`.
- File tạm Edge-TTS được xóa sau yêu cầu; MP3 hoàn tất và metadata/checksum được lưu bền trong App_Data/audio (Git ignore). Hủy request sẽ kết thúc tiến trình Python con. Trình duyệt giữ Blob để phát lại và thu hồi URL khi đổi địa điểm/ngôn ngữ hoặc đóng component.

## Kịch bản trình diễn

1. Mở trang, xem ba địa điểm được tải qua API.
2. Tìm `vinh khanh` để kiểm tra tìm kiếm không dấu; xóa bộ lọc.
3. Chọn địa điểm, chọn Tiếng Việt, nhấn Nghe thuyết minh; thử dừng và phát lại.
4. Chọn English, chờ nội dung đã dịch rồi nghe. Thử các ngôn ngữ khác.
5. Đổi địa điểm hoặc ngôn ngữ khi đang tạo âm thanh: yêu cầu cũ phải bị hủy.
6. Tắt backend, tải lại frontend: thông báo kết nối và nút Thử lại xuất hiện. Khởi động backend và thử lại.

## Phạm vi dữ liệu

Ba địa điểm có nhãn `isDemo=true`. Thuyết minh vẫn là nội dung kiểm thử, chưa phải bài lịch sử chính thức. Ảnh thật có chú thích và nguồn; địa chỉ lấy từ nguồn chính thức/báo chí. Bến Nhà Rồng và Chợ Xóm Chiếu có vị trí tham khảo từ OpenStreetMap, không cam kết là tọa độ cổng vào đo GPS. Vĩnh Khánh chưa có tọa độ đã xác minh nên không đặt ghim.

## Admin và chủ quán

Mở `/admin` hoặc `/owner`. Tài khoản admin ban đầu nằm trong `ShortVideoTranslator.Api/App_Data/bootstrap-admin.txt` (ignored); người dùng phải đổi mật khẩu lần đầu. Không gửi file này lên Git hoặc đưa vào báo cáo. Cookie HttpOnly/Strict, CSRF, phiên truy cập 30 phút, refresh token xoay vòng 7 ngày; hỗ trợ Bearer cho client API. Production cần `Security__JwtSecret` từ 32 ký tự và `Admin__BootstrapPassword` cho lần khởi tạo.

Admin quản lý địa điểm, ảnh/crop, thực đơn, tài khoản, vai trò, kiểm duyệt đăng ký/đề xuất, nhật ký và tác vụ audio. Quyền được kiểm tra ở backend. Chủ quán phải được duyệt, chỉ truy cập địa điểm và đề xuất của mình. Một JSON store local lưu dữ liệu bền bằng ghi file nguyên tử; phù hợp demo một tiến trình, chưa thay thế database production.

Địa điểm chính thức (`isDemo=false`) yêu cầu nguồn. Khi yêu cầu công khai, hệ thống giữ ẩn và tự xếp tác vụ tiếng Anh trước; chỉ công khai sau khi dịch và MP3 tiếng Anh cho đúng phiên bản mô tả đã hoàn tất. Sửa mô tả làm mất trạng thái sẵn sàng cũ. Dữ liệu demo được miễn gate này và luôn có nhãn.

## Offline và GPS

`npm run build` rồi `npm run preview` bật Service Worker production tại 5173. Giao diện, các ảnh đã xem, dữ liệu API và MP3 đã tải có thể mở lại khi mất kết nối. Đổi ngôn ngữ tự chuẩn bị bundle UI và nội dung/MP3 cho tối đa 3 địa điểm ưu tiên; chỉ mở nút nghe khi đủ cache. Nút **Chuẩn bị ngôn ngữ** dùng để thử lại (gần vị trí trong 1.5 km khi được cấp GPS). Nút **Tải audio offline** tải MP3 đã tạo sẵn, xác minh SHA-256 và báo số nội dung còn thiếu. Cache audio LRU giới hạn 3 ngôn ngữ, 300 file/ngôn ngữ, xử lý quota bằng dọn file ít dùng. Không tải hàng loạt raster tile OSM. Bản đồ online cần Internet. Gói PMTiles Quận 1 đã tạo: nhấn **Tải bản đồ Quận 1 offline**, chờ xác minh, rồi chọn Offline. Hybrid dùng cloud trước và fallback pack khi trình duyệt offline/ô map lỗi; probe15 giây trước khi phục hồi. Style offline giản lược không nhãn đường. Nếu clone sang máy khác, chạy `pwsh -File scripts/Prepare-MapPack.ps1` để tái tạo gói; không commit file PMTiles/công cụ.

GPS chỉ hoạt động khi người dùng bật và cấp quyền: throttle 5 giây, debounce 3 giây, reconcile 5 giây, cooldown 5 phút, bỏ vị trí có độ chính xác trên 100 m. Prefetch tối đa 3 POI trong 500 m mỗi 30 giây, có backoff khi 429/lỗi. Chưa kiểm thử thực địa; autoplay có thể bị trình duyệt chặn và cần nút phát thủ công.

UI công khai có bundle tĩnh 6 ngôn ngữ độc lập với bản dịch thuyết minh. Một số thông báo chi tiết/cổng quản trị còn tiếng Việt. AI Gemini chỉ hoạt động khi có `Ai__ApiKey` và `Ai__Model`; mặc định báo 503 chưa sẵn sàng. Không tạo nội dung AI giả.

Đối chiếu đầy đủ file HTML và các phần chưa triển khai: [IMPLEMENTATION-STATUS.md](IMPLEMENTATION-STATUS.md).

## Lệnh kiểm tra

```powershell
dotnet build
dotnet test
cd ShortVideoTranslator.Frontend
npm run build
npm run lint
```

Không đưa `models`, `node_modules`, `bin`, `obj`, `.env`, khóa bí mật hay MP3 thử nghiệm lên Git. Giữ các quy tắc ignore đã có. Không chạy `git push --force` hoặc push vào main.

## Giám sát vị trí và xác minh chủ quán

Giám sát vị trí tách khỏi analytics. Checkbox chia sẻ mặc định tắt mỗi lần mở trang; GPS chính xác chỉ phục vụ geofence trên thiết bị, dữ liệu gửi server được làm tròn ~100m. Admin có system:logs xem các cửa sổ 1/6/24 giờ. Tắt chia sẻ/xóa sẽ thu hồi phiên và xóa dữ liệu, không nhận lại request muộn của cùng phiên. Dữ liệu không gắn tài khoản và hết hạn 24 giờ.

Owner có mục **Xác minh chủ quán** kể cả khi đang chờ duyệt. Giấy tờ là tùy chọn, lưu mã hóa, owner chỉ thấy 4 số cuối; Admin cần quyền user:read và poi:approve để xem khi duyệt đăng ký. Có audit truy cập, không đưa số vào audit/danh sách tài khoản. Hết đọc đúng 180 ngày; dọn ciphertext mỗi giờ. Demo dùng dữ liệu giả lập, không yêu cầu giấy tờ thật.

Ngày14/10/2026 đã xác nhận3 tác vụ Admin completed 6/6 (vi/en/zh/ja/ko/fr). Edge-TTS 7.2.8 phát sinh403 và được cập nhật7.3.1. Một số task thất bại cũ vẫn hiển thị để giữ lịch sử; phân biệt bằng thời gian tạo.


## Phạm vi du lịch hiện tại: Quận 1

Trang khám phá chỉ phục vụ Quận1: Chợ Bến Thành, Dinh Độc Lập, Bưu điện Trung tâm Sài Gòn, Phố đi bộ Nguyễn Huệ và Nhà hát Thành phố. Giữ nhãn demo cho các giới thiệu ngắn có nguồn; không thay bằng bài lịch sử chưa kiểm chứng. Khi mở dữ liệu cũ, migration gán bản ghi cũ khu vực district4, giữ toàn bộ nội dung/tài khoản/review/menu, rồi thêm năm địa điểm mới bằng IDs chưa sử dụng (migration phiên bản4 bổ sung hai nơi cho kho đã có ba nơi Quận1). Admin vẫn thấy kho cũ nhưng trạng thái ghi rõ ngoài phạm vi. Không đổi khu vực bản ghi cũ nếu địa điểm thực tế không thuộc Quận1.

Bộ nhớ catalog/map của district1 độc lập nên không hiện lại danh sách hoặc gói Quận4. Nguồn địa chỉ, ảnh, giấy phép và tọa độ: [DISTRICT1-SOURCES.md](DISTRICT1-SOURCES.md). “Quận1” là cách gọi khu vực du lịch quen thuộc; địa chỉ phường mới chỉ ghi khi nguồn chính thức xác nhận. Khung xem map quanh các công trình không được coi là ranh giới hành chính.
