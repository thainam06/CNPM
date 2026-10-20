# Báo cáo demo SmartTour Guide

Hệ thống thuyết minh tự động đa ngôn ngữ cho địa điểm du lịch Quận 1

Đề tài Thuyết minh tự động đa ngôn ngữ cho địa điểm du lịch Quận 1

Hướng triển khai Backend ba lớp có CI CD

Tài liệu phản ánh bản demo hiện có, phục vụ đối chiếu chức năng, trình diễn và giải thích xử lý hệ thống. Những hành vi chưa kiểm chứng được ghi riêng; tài liệu không xác nhận hoàn thành toàn bộ HTML mẫu.

Môn học: Công nghệ phần mềm

Trường và lớp: ______________________________

Giảng viên: ________________________________

Thành viên 1: __________________  MSSV: __________

Thành viên 2: __________________  MSSV: __________

Thành viên 3: __________________  MSSV: __________

Phiên bản tài liệu 1.0    Ngày lập 20/10/2026

Thông tin nhóm để trống do chưa được cung cấp. Phân công trong báo cáo là đề xuất, không phải xác nhận ai đã thực hiện mã nguồn.

## Nội dung báo cáo

1 Giới thiệu và phạm vi

2 Yêu cầu và phương pháp thực hiện

3 Kiến trúc và tổ chức mã nguồn

4 Dữ liệu và mô hình lưu trữ

5 Chức năng trang khám phá

6 Quy trình dịch và tạo thuyết minh

7 Quản trị và cổng chủ quán

8 Offline bản đồ và GPS

9 Xác thực và bảo vệ dữ liệu

10 CI CD và cách chạy

11 Kịch bản trình diễn

12 Kiểm thử và bằng chứng

13 Hạn chế và công việc tiếp theo

14 Phân công đề xuất và kết luận

15 Nguồn đối chiếu và phụ lục

Phạm vi bằng chứng: build và test được chạy lại ngày 20/10/2026; kiểm tra browser, MP3 và offline dẫn từ đợt 18/10. Ảnh có chú thích thời điểm và chỉ minh họa trạng thái ghi nhận, không thay thế kết quả kiểm thử mới.

Cách dùng: có thể copy nội dung từ file Markdown đi kèm. Nhóm điền thông tin trường, lớp và thành viên, sau đó cập nhật kết quả demo tại buổi bảo vệ. Không đưa mật khẩu, token, khóa API hoặc giấy tờ cá nhân vào bản nộp.

## 1 Giới thiệu và phạm vi

SmartTour Guide được xây dựng để người tham quan lựa chọn địa điểm tại khu vực Quận 1, đọc thông tin có nguồn và nghe thuyết minh bằng ngôn ngữ phù hợp. Bản demo sử dụng React và Vite cho giao diện; ASP.NET Core 10 và C# cho Web API, nghiệp vụ và truy cập dữ liệu.

Đồ án chọn hướng Backend theo kiến trúc ba lớp kèm CI/CD. Hệ thống tận dụng project ShortVideoTranslator hiện có, giữ tên Solution và namespace, đồng thời bổ sung nghiệp vụ du lịch. Chức năng video/Whisper cũ được giữ lại nhưng không nằm trong kịch bản demo du lịch và không được dùng làm bằng chứng hoàn tất đề tài mới.

### Mục tiêu bản demo

• Khám phá năm địa điểm có tên, ảnh, địa chỉ, nguồn và bản đồ; dữ liệu lấy từ API.

• Thuyết minh vi, en, zh, ja, ko và fr bằng nội dung đúng ngôn ngữ trước khi tổng hợp MP3.

• Quản trị dữ liệu, tài khoản, vai trò, kiểm duyệt chủ quán và đề xuất theo quyền.

• Chuẩn bị dữ liệu để mở lại và nghe MP3 đã tải khi máy chủ ứng dụng không truy cập được.

• Có kiểm thử tự động và pipeline build, test, đóng gói để hỗ trợ bàn giao.

### Phạm vi và kết quả chính

Bản demo cốt lõi đã có bằng chứng chạy thực tế; chưa phải sản phẩm hoàn tất toàn bộ khả năng nâng cao trong HTML. Toàn bộ năm POI vẫn có nhãn demo. Nội dung ngoại ngữ soạn sẵn có ghi SmartTourDraft, không nhận là dịch MyMemory trực tiếp hoặc bản đã được chuyên gia hiệu đính.

Phạm vi được thay từ Quận 4 sang Quận 1 theo chỉ đạo của người dùng. Thiết kế sử dụng cam, trắng kem và nâu nhạt; không thay đổi stack sang FastAPI/MongoDB chỉ để khớp tên công nghệ trong HTML mẫu.

## 2 Yêu cầu và phương pháp thực hiện

| Nhóm yêu cầu | Phương án hiện tại | Trạng thái |
| --- | --- | --- |
| Khám phá POI | Catalog API, tìm kiếm không dấu, phân loại, chi tiết và ảnh. | Có triển khai |
| Đa ngôn ngữ | Nguồn Việt, bản duyệt hoặc bản mẫu khớp nguồn, MyMemory cho nguồn khác. | Có dịch thật |
| Text to Speech | C# gọi Python Edge-TTS, cache MP3 và giọng theo locale. | Có MP3 thật |
| Admin và Owner | CRUD, RBAC, duyệt, thông báo, menu, audit và chẩn đoán. | Có triển khai |
| Offline và GPS | SW/IDB/PMTiles; GPS tự bật và chia sẻ theo consent riêng. | Có giới hạn |
| AI tùy chọn | Gemini/ProxyPal và OpenWeather có adapter. | Chưa cấu hình thật |
| Ba lớp và CI CD | Api, Business, Data; workflow build/test/publish artifact. | Cần hoàn thiện ranh giới và deploy |

Phương pháp thực hiện gồm khảo sát mã hiện có, đối chiếu HTML theo hành vi, triển khai từng nhóm chức năng và kiểm thử. Phần được mô tả là đã làm phải có đường xử lý backend hoặc client rõ ràng; thành công bằng fixture không được trình bày là đã gọi nhà cung cấp ngoài mạng.

Yêu cầu phi chức năng trong phạm vi demo: báo lỗi rõ, hủy request cũ khi đổi lựa chọn, không phát chồng audio do thao tác thủ công, kiểm tra quyền phía server, lưu dữ liệu nguyên tử và bảo vệ secret khỏi Git. Chưa có kiểm thử tải, tính sẵn sàng nhiều máy chủ hoặc chứng nhận bảo mật production.

Dùng 28 Use Case trong tài liệu đặc tả đi kèm để truy vết yêu cầu. Các tiêu chí mở rộng như long-tail UI translation, nhiều scope map và đầy đủ bốn tầng audio được ghi là còn thiếu, không gộp vào danh sách hoàn tất.

## 3 Kiến trúc và tổ chức mã nguồn

Hình 1   Kiến trúc và luồng xử lý mục tiêu của bản demo

| Project | Trách nhiệm |
| --- | --- |
| ShortVideoTranslator.Api | HTTP controller, xác thực/CSRF, DI và hạ tầng worker/API. |
| ShortVideoTranslator.Business | Mô hình nghiệp vụ, service, kiểm tra quy tắc, dịch và TTS. |
| ShortVideoTranslator.Data | Repository mẫu, JsonPlatformStore và migration bảo toàn dữ liệu. |
| ShortVideoTranslator.Frontend | React, gọi API, audio, bản đồ, Portal và cache thiết bị. |
| ShortVideoTranslator.Tests | xUnit và integration tests bằng WebApplicationFactory/kho thử. |

Luồng điển hình là PlacesController → PlaceService → IPlaceRepository → JsonPlatformStore. Interface được đăng ký qua DI. Tuy nhiên, một số controller vẫn gọi Store.Read/Write và chứa xử lý nghiệp vụ; vì vậy báo cáo chỉ xác nhận cấu trúc theo hướng ba lớp, không khẳng định đã tách hoàn toàn.

## 4 Dữ liệu và mô hình lưu trữ

| Địa điểm demo | Nội dung và ảnh | Ghi chú |
| --- | --- | --- |
| Chợ Bến Thành | Bài Việt 906 ký tự; có 2 ảnh. | Chợ truyền thống |
| Dinh Độc Lập | Bài Việt 981 ký tự; có 2 ảnh. | Di tích lịch sử |
| Bưu điện Trung tâm Sài Gòn | Bài Việt 962 ký tự; có 2 ảnh. | Kiến trúc |
| Phố đi bộ Nguyễn Huệ | Bài Việt 957 ký tự; có 2 ảnh. | Không gian công cộng |
| Nhà hát Thành phố | Bài Việt 968 ký tự; có 2 ảnh. | Văn hóa và nghệ thuật |

Năm bài Việt được biên soạn độc lập để demo; thông tin nguồn và mức độ xác thực được dẫn trong NARRATION-CONTENT.md và DISTRICT1-SOURCES.md. Năm bài có 25 bản ngoại ngữ soạn sẵn, chỉ dùng khi nội dung Việt khớp chính xác. Ảnh có credit/giấy phép và năm chụp khi có dữ liệu; không gọi tất cả ảnh là ảnh mới nhất.

| Thực thể | Quan hệ và dữ liệu chính |
| --- | --- |
| Place và MenuItem | Một địa điểm có nhiều món; POI có owner tùy chọn và nhiều PlacePhoto. |
| PlatformUser và PlatformRole | User thuộc một role; role chứa mảng permission; mật khẩu chỉ lưu hash. |
| Review và OwnerNotification | Review thuộc người gửi; notification liên kết review và POI khi có. |
| TranslationDraft và ApprovedTranslation | Draft theo POI/ngôn ngữ; bản công bố theo sourceHash/ngôn ngữ/revision. |
| AudioTaskState và NarrationAsset | Task giữ trạng thái/nguồn; MP3 có metadata, bytes và SHA-256. |
| Analytics và RuntimeLocation | Hai luồng consent riêng, không gộp GPS vào lượt xem/nghe. |

platform.json được ghi qua snapshot và thay file nguyên tử, phù hợp demo một tiến trình. App_Data giữ JSON, MP3, media và PMTiles; IndexedDB của trình duyệt lưu dữ liệu offline. Chưa dùng cơ sở dữ liệu quan hệ hoặc hệ lưu trữ phân tán.

## 5 Chức năng trang khám phá

Trang chủ có phần giới thiệu, chọn ngôn ngữ hành trình, danh sách có tìm kiếm/bộ lọc, chi tiết, bản đồ và tiện ích offline. React lấy catalog và chi tiết qua API; không dùng danh sách địa điểm cố định làm nguồn lâu dài.

Hình 2   Giao diện khám phá lưu từ giai đoạn trước   Ảnh chỉ minh họa bố cục

Khi chọn địa điểm, ứng dụng dừng audio cũ, hủy request trước rồi lấy nội dung mới. Ảnh bìa có tên phủ trên ảnh và trọng tâm crop; gallery chuyển mỗi 3 giây nếu đang hiển thị và không bị hover/focus. Địa chỉ và nguồn được hiển thị riêng. Các Use Case tương ứng là UC01, UC02 và UC03.

Ở chế độ English, các nhãn chính đổi ngôn ngữ nhưng một số hướng dẫn vận hành, thông báo và nội dung bổ sung vẫn Việt. Đây là giới hạn cần bổ sung, không trình bày là giao diện đã bản địa hóa toàn bộ.

## 6 Quy trình dịch và tạo thuyết minh

Luồng xử lý: chọn địa điểm → lấy nguồn Việt → chọn ngôn ngữ → chuẩn bị bản dịch → lấy cache hoặc gọi Edge-TTS → nhận MP3 → phát trên trình duyệt. Frontend gửi nguồn gốc cho backend, không dùng cách đọc nguyên tiếng Việt bằng giọng ngoại ngữ.

| Nguồn bản dịch | Điều kiện sử dụng | Thông báo cho người dùng |
| --- | --- | --- |
| Original | Ngôn ngữ vi. | Nội dung Việt gốc. |
| Reviewed | Có bản Admin công bố khớp sourceHash/lang. | Bản đã biên tập và duyệt trong hệ thống. |
| SmartTourDraft | Có bản mẫu khớp chính xác nguồn Việt. | AI hỗ trợ soạn, chưa hiệu đính chuyên môn. |
| MyMemory | Nguồn khác, dịch vụ bật và có mạng/quota. | Dịch tự động, có thể sai ngữ nghĩa hoặc tên riêng. |

TextToSpeechService chọn HoaiMyNeural cho Việt; JennyNeural, XiaoxiaoNeural, NanamiNeural, SunHiNeural và DeniseNeural cho các ngoại ngữ tương ứng. Python được gọi bằng ArgumentList, có timeout/cancellation và dọn file tạm. Audio được cache theo nguồn, ngôn ngữ và revision bằng SHA-256.

NarrationAssetService kiểm tra bytes và checksum trước khi dùng cache; tối đa ba TTS đồng thời. Bản Admin duyệt có revision riêng; thay nguồn làm bản cũ không còn phù hợp. UI có trạng thái loading, ready, playing, stopped; autoplay bị chặn được hướng dẫn phát thủ công.

Bằng chứng ngày 18/10: có 30 MP3 của năm nguồn mới; cả 30 file khớp SHA-256 và ffprobe đọc được codec/thời lượng. Bến Thành Việt 56,352 giây và Anh 50,496 giây phát trên browser với readyState=4, paused=false, error=null. Một nguồn kỹ thuật mới cũng dịch MyMemory và tạo MP3 Việt/Anh thành công. Không dùng dữ liệu cache cũ để chứng minh probe mới.

Các giới hạn: cần Internet để tạo audio/dịch mới, Edge-TTS và dịch miễn phí có thể lỗi hoặc thay đổi hạn mức; chưa có voice catalog động và đầy đủ bốn tầng fallback như HTML. Nhóm chỉ công bố những giọng và locale đã ánh xạ trong source.

## 7 Quản trị và cổng chủ quán

| Nhóm màn hình | Chức năng |
| --- | --- |
| Địa điểm và thực đơn | CRUD POI, nguồn/địa chỉ/ảnh, chủ sở hữu, menu và trạng thái công khai. |
| Tài khoản và vai trò | Tạo/sửa/khóa/mở/xóa theo quyền; 4 role mặc định và 32 permission. |
| Kiểm duyệt và thông báo | Duyệt/từ chối đăng ký hoặc đề xuất; ghi chú và inbox của đúng chủ quán. |
| Bản dịch và tác vụ audio | Nháp/duyệt/thu hồi; queue và điều khiển audio, SSE cập nhật tiến độ. |
| Vận hành | Tổng quan/CSV, audit, runtime window, chẩn đoán và đổi mật khẩu. |

Chủ quán đăng ký tạo user chưa xác minh và review pending. Trước duyệt chỉ xem trạng thái, thông báo và thông tin xác minh tùy chọn; không quản lý POI. Sau duyệt, chủ quán gửi đề xuất cho địa điểm thuộc mình. Admin quyết định rồi hệ thống tạo notification có ghi chú. Hồ sơ bị từ chối có thể gửi lại một lần pending mới, giữ lịch sử cũ.

POI chính thức có publication gate English/audio đúng nguồn; bản demo được miễn gate nhưng có nhãn. Bản dịch nháp không thay nội dung khách nghe. Duyệt bản dịch tạo MP3 trước; lỗi TTS giữ bản công khai trước đó. Các chức năng này tương ứng UC11 đến UC28.

AI Advisor chỉ gợi ý nội dung để người quản trị rà soát rồi áp dụng vào biểu mẫu. Hiện chưa cấu hình provider thật nên nút tạo bị vô hiệu và có thông báo; không sử dụng phản hồi giả để trình diễn. Có test adapter/quota/fallback bằng fixture, không đồng nghĩa kết nối thật đã thành công.

## Minh họa kiểm tra hệ thống quản trị

Hình 3   Chẩn đoán Admin ghi nhận ngày 15 tháng 10 năm 2026   Không phải ảnh chạy lại ngày lập báo cáo

Màn hình phân biệt kho cốt lõi, worker, map và cấu hình provider. “Đã cấu hình” chỉ nói về cấu hình, không chứng minh kết nối ngoài mạng. Bảng audio được kiểm theo nguồn hiện tại. Vì vậy người trình diễn phải phân biệt readiness kho dữ liệu với kết quả tạo MP3 mới hoặc deploy thành công.

## 8 Offline bản đồ và GPS

PWA chỉ đăng ký service worker ở production. SW lưu shell và hashed assets theo build, tránh trộn HTML với chunk khác phiên bản. Catalog, bản dịch, MP3 và PMTiles dùng IndexedDB; ảnh đã xem còn có cache riêng. Gói đầy đủ được staging và xác minh trước khi commit.

Gói Việt ngày 18/10 có 5 POI, 10 ảnh, 5 MP3 và bản đồ, khoảng 13,7 MB. Sau khi dừng preview 5175, trang vẫn reload, đọc ảnh blob local, render MapLibre và phát bài Việt 56,352 giây. Máy chủ API chính vẫn chạy nhưng không thể gọi qua proxy preview đã dừng. Đây là kiểm tra mất kết nối máy chủ ứng dụng, chưa phải ngắt Internet toàn máy.

PMTiles Quận 1 có 1.914.259 byte, nguồn Protomaps/OpenStreetMap và metadata nguồn/giấy phép/checksum. Map online dùng Leaflet; offline dùng MapLibre. Style offline giản lược không nhãn đường. Iframe OSM ở chi tiết vẫn là tài nguyên online. Hiện chỉ hỗ trợ một scope Quận 1.

GPS mặc định tắt. Khi khách bật, hệ thống kiểm độ mới, sai số, throttle 5 giây, debounce 3 giây, radius mặc định 30 m, reconcile 5 giây và cooldown 5 phút. Nearby prefetch giới hạn ba điểm. Vị trí ở máy không tự được gửi tới Admin; chia sẻ runtime cần consent riêng.

Lỗi đã biết: trong hai vùng chồng nhau, engine có thể chọn điểm ưu tiên cao tại giây 3 rồi điểm ưu tiên thấp tại giây 8; App chuyển POI và dừng audio trước. Chưa có hàng đợi phát single-slot hoàn chỉnh. Chưa thử đi bộ thực địa và cổng vào công trình; tọa độ hiện là vị trí tham khảo có nguồn.

Chưa kiểm chứng đầy bộ nhớ, tất cả sáu gói ngôn ngữ, update SW nhiều phiên bản hoặc hybrid chuyển đi/chuyển lại cloud trên thiết bị thật. Vì vậy không công bố “hoạt động hoàn toàn offline trong mọi trường hợp”.

## 9 Xác thực và bảo vệ dữ liệu

Browser dùng cookie HttpOnly/SameSite Strict và CSRF cho mutation. Access có hạn 30 phút; refresh token ngẫu nhiên được hash ở server, xoay vòng và hết hạn 7 ngày. Có Bearer API lane riêng. Đổi mật khẩu, khóa tài khoản hoặc đổi role làm security stamp cũ mất hiệu lực.

| Quy tắc | Cách áp dụng |
| --- | --- |
| Phân quyền | Backend kiểm permission, không chỉ ẩn nút React. Không cấp quyền vượt quyền người thao tác. |
| Ownership | Owner chỉ truy cập POI/menu/notification của chính mình; verified gate áp dụng nghiệp vụ. |
| Mật khẩu | PBKDF2 với salt, không trả hash qua API; 7–128 ký tự. Không ghi mật khẩu demo vào báo cáo. |
| Giấy tờ tùy chọn | Data Protection, mask cho owner, quyền riêng khi Admin đọc, hết đọc sau 180 ngày. |
| Consent analytics | Chỉ thu view/play khi đồng ý; có forget và chặn request muộn sau thu hồi. |
| Consent runtime | Tách khỏi analytics, làm tròn khoảng 100 m, lưu tối đa 24 giờ. |
| Git | Ignore App_Data, secret, media/audio, node_modules, bin/obj và model Whisper. |

Các kiểm thử bảo mật sử dụng fixture và dữ liệu giả lập. Không dùng số CCCD thật, vị trí thật của khách hoặc token trong minh chứng. Hệ thống chưa qua đánh giá an ninh production hay kiểm thử tải. JSON và key ring cần phương án lưu trữ phù hợp nếu triển khai sang máy khác.

## 10 CI CD và cách chạy

Workflow .github/workflows/dotnet-ci.yml khai báo chạy khi push main/feature/** hoặc PR vào main. Các bước gồm checkout, .NET 10 restore/build/test, Node 24 npm ci/lint/test/build, dotnet publish và upload artifact Backend. Chưa xác minh run GitHub mới; workflow chưa có bước deploy lên môi trường hoặc health check sau deploy.

Vì vậy báo cáo ghi nhận CI được cấu hình và đóng gói artifact, chưa ghi CD triển khai tự động hoàn tất. Ảnh yêu cầu số 2 của giảng viên nêu Backend ba lớp kèm CI/CD; phần này cần bằng chứng run và làm rõ mức bàn giao artifact hay deploy khi chốt nộp.

### Chạy Backend trong PowerShell

cd C:\CNPM\ShortVideoTranslator

$env:TextToSpeech__PythonArguments__0 = "-3.14"

dotnet run --project ShortVideoTranslator.Api --launch-profile http

API dự kiến http://localhost:5132. Mở /api/places để kiểm tra JSON; giữ terminal hoạt động.

### Chạy Frontend ở terminal riêng

cd C:\CNPM\ShortVideoTranslator\ShortVideoTranslator.Frontend

npm.cmd ci    (chỉ cần khi chưa có dependency hoặc lockfile đổi)

npm.cmd run dev

Mở http://localhost:5173, /admin và /owner. Dùng npm.cmd để tránh npm.ps1 bị PowerShell chặn. Cài TTS vào đúng Python bằng py -3.14 -m pip install -r ShortVideoTranslator.Api/requirements-tts.txt.

Demo PWA: dừng dev bằng Ctrl+C, chạy npm.cmd run build rồi npm.cmd run preview; không chạy dev và preview cùng cổng 5173. Tài khoản dùng thông tin đã được nhóm cấu hình; bootstrap-admin.txt chỉ là thông tin khởi tạo, không bảo đảm mật khẩu hiện tại.

## 11 Kịch bản trình diễn

Thời lượng gợi ý 12–15 phút, chưa phải số đo hiệu năng. Chuẩn bị hai terminal, MP3 sẵn có, tai nghe/loa và tài khoản đúng quyền. Thao tác ghi/xóa chỉ dùng kho thử; giữ năm POI mẫu để trình diễn.

| Bước | Thao tác | Kết quả cần chỉ ra |
| --- | --- | --- |
| 1 | Mở API /api/places và trang chủ. | Backend thật; 5 địa điểm Quận 1. |
| 2 | Chọn ngôn ngữ ở hộp thoại, mở danh sách. | Readiness và trạng thái tải, không giả sẵn sàng. |
| 3 | Tìm nguyen hue rồi xóa tìm kiếm. | Tìm không dấu và bộ lọc hoạt động. |
| 4 | Chọn POI, xem địa chỉ/nguồn và bộ ảnh. | Đúng địa điểm, ảnh có credit và điều khiển. |
| 5 | Nghe Việt, dừng và phát lại. | MP3 thật, trạng thái audio đúng. |
| 6 | Đổi English và nghe. | Nội dung dịch khác Việt; ghi đúng provider. |
| 7 | Đổi địa điểm khi đang nghe. | Bài cũ dừng, không nhận kết quả request cũ. |
| 8 | Mở bản đồ và chọn ghim. | Chi tiết và lựa chọn đồng bộ. |
| 9 | Đăng nhập Admin, mở các tab. | Quyền, dữ liệu và tác vụ có backend. |
| 10 | Trên kho thử: owner đăng ký/gửi đề xuất. | Trước duyệt chưa có quyền; Admin xem và duyệt. |
| 11 | Owner đọc notification và đánh dấu đã đọc. | Ghi chú, người nhận và số chưa đọc đúng. |
| 12 | Mở tác vụ audio hoặc bản dịch. | Phân biệt task thật, draft và bản công bố. |
| 13 | Production tải gói offline Việt. | Đợi xác minh; hiển thị số POI/ảnh/MP3. |
| 14 | Mô phỏng mất máy chủ trên môi trường thử. | Reload, ảnh/map/audio đã lưu vẫn dùng được. |
| 15 | Mở chẩn đoán và kết thúc. | Nêu AI chưa cấu hình, GPS chưa thực địa và CI chưa deploy. |

## 12 Kiểm thử và bằng chứng

| Kiểm tra | Ngày | Kết quả và phạm vi |
| --- | --- | --- |
| dotnet build | 20/10/2026 | Thành công, 0 warning và 0 error. |
| dotnet test --no-build | 20/10/2026 | 105/105 đạt; bao gồm unit và HTTP integration với kho thử. |
| npm.cmd run lint | 20/10/2026 | Không có lỗi lint. |
| npm.cmd test | 20/10/2026 | 34/34 Node tests; không phải 34 browser E2E. |
| npm.cmd run build | 20/10/2026 | Thành công; chunk OfflineMap khoảng 1.076,59 KB, gzip 293,98 KB. |
| HTTP POI và CORS | 18/10/2026 | 5 POI, detail/nearby, ID sai 404, ETag 304, CORS đúng. |
| MP3 hiện tại | 18/10/2026 | 30/30 file HTTP bytes/SHA-256 đúng, ffprobe đọc codec và duration. |
| Browser nghe | 18/10/2026 | Việt 56,352 s; Anh 50,496 s; readyState 4, không lỗi audio. |
| TTS nguồn mới | 18/10/2026 | MyMemory en 200; Edge-TTS vi/en 200. Không phải hit nguồn mẫu. |
| CRUD và Owner | 18/10/2026 | HTTP kho riêng: role/user/POI/menu, review và notification đạt. |
| Offline production | 18/10/2026 | Gói Việt 13,7 MB tải/reload/phát được khi preview tắt. |
| AI và weather | Fixture | Adapter/quota/fallback có tests; chưa chứng minh provider thật. |

Không cộng 105 và 34 để suy ra tỷ lệ yêu cầu hay code coverage. Test đạt không chứng minh tất cả nhánh xử lý, thiết bị hoặc kết nối bên ngoài đều đạt. Một số tests dùng byte MP3 giả lập để kiểm ACL/cache; bằng chứng MP3 thật được ghi riêng ở trên.

Tiêu chí nghiệm thu demo: nguồn/locale/audio khớp, dữ liệu lấy qua API, mutation kiểm quyền, không lộ secret và lỗi có thông báo. Chưa kiểm tra đầy bộ nhớ, mạng treo kéo dài, Safari/iOS/Opera thật, tải nhiều người dùng hoặc deploy production.

## 13 Hạn chế và công việc tiếp theo

| Ưu tiên | Vấn đề | Công việc cần làm |
| --- | --- | --- |
| Cao | Geofence vùng chồng lấn có thể ngắt bài. | Thêm queue có ưu tiên và kiểm bài đang phát. |
| Cao | Catalog thiếu timeout; menu catch rỗng. | Snapshot sớm, deadline hợp lý, hiển thị lỗi menu. |
| Cao | Lifecycle xóa và task stale chưa đủ. | Cleanup dữ liệu/file phụ, xử lý retry/stale và test lỗi giữa chừng. |
| Vừa | Gallery tổng 7 thay vì 8 ảnh theo mẫu. | Thống nhất backend và UI với yêu cầu ảnh. |
| Vừa | UI còn tiếng Việt khi đổi locale. | Hoàn thiện bộ nhãn/hướng dẫn, không dịch sai tên riêng. |
| Vừa | AI provider chưa cấu hình thật. | Cấu hình và kiểm tra thật nếu thuộc phạm vi bảo vệ. |
| Theo phạm vi HTML | Long-tail UI, nhiều scope map, đầy đủ fallback. | Bổ sung hoặc thống nhất phạm vi với giảng viên. |
| Trước nộp | Ba lớp, CI/CD và clone sạch chưa chốt. | Bỏ logic/store khỏi controller; chứng minh workflow và bàn giao đủ file. |

GPS chưa đo thực địa; tọa độ hiện là vị trí tham khảo. Bản đồ chi tiết iframe còn phụ thuộc Internet. Kho JSON phù hợp demo một tiến trình; chưa có read model thống kê phân tán. Dữ liệu demo và bản dịch chưa được nhận là tài liệu chính thức của đơn vị quản lý.

Đánh giá 80 tiêu chí ngày 18/10 ghi nhận 52 đã có bằng chứng, 22 chưa hoàn thiện/chưa kiểm chứng và 6 chưa làm; tỷ lệ nghiêm ngặt là 65%. Đây là tỷ lệ theo toàn bộ HTML và yêu cầu mở rộng, không phải điểm đánh giá riêng cho hướng Backend ba lớp, không phải tỷ lệ test coverage và không phải điểm giảng viên.

## 14 Phân công đề xuất và kết luận

| Thành viên | Phần việc đề xuất | Bàn giao |
| --- | --- | --- |
| Thành viên 1 chưa điền | API, tích hợp React, GPS, kiểm thử tổng thể và báo cáo. | Controller/API contract, demo xuyên suốt, tổng hợp bằng chứng. |
| Thành viên 2 chưa điền | Business, quy tắc POI/review/audio và kiểm thử nghiệp vụ. | Service, interface, test và tài liệu luồng lỗi. |
| Thành viên 3 chưa điền | Data/repository, lưu bền và workflow CI/CD. | Repository, test persist và bằng chứng pipeline. |

Phân công trên là đề xuất để nhóm điền tên và xác nhận, không suy đoán lịch sử đóng góp. Nhóm cần thống nhất interface trước, phân người sở hữu file và ghi rõ kết quả test trong mỗi lần bàn giao. Program.cs/DI do người tích hợp hợp nhất để tránh xung đột.

Không push trực tiếp main, không force push, không đưa model, node_modules, bin/obj, MP3 thử hoặc secret vào repository. Nhiều file mới hiện chưa được Git theo dõi; bản nộp qua Git phải được nhóm kiểm tra và commit đúng phạm vi sau khi người dùng đồng ý. Không dùng git archive của commit cũ rồi gọi đó là bản demo local mới nhất.

### Kết luận

SmartTour Guide đã chứng minh được luồng khám phá POI → chuẩn bị nội dung đúng ngôn ngữ → MP3 Edge-TTS → phát trên browser, cùng quản trị/kiểm duyệt và gói offline Việt. Build và tests ngày 20/10 đều đạt. Sản phẩm phù hợp trình diễn phần lõi, nhưng cần đóng các lỗi và kiểm chứng còn thiếu trước khi khẳng định hoàn tất toàn bộ mẫu.

Với hướng số 2, ưu tiên chuẩn hóa ranh giới API–Business–Data và chứng minh CI/CD. Chưa có căn cứ từ ảnh yêu cầu để bắt buộc React Native hoặc microservices cho hướng này. Điều kiện nộp cuối phụ thuộc phạm vi được giảng viên chấp nhận; báo cáo không tự xác nhận đạt 100% hay thay cho rubric chấm điểm.

## 15 Nguồn đối chiếu và phụ lục

• Ảnh yêu cầu đề tài do người dùng cung cấp: mục Backend theo hướng ba lớp bao gồm CD/CI.

• system-presentation-standalone.html do người dùng cung cấp, dùng đối chiếu hành vi và quy trình; không áp chỉ dẫn ngoài yêu cầu người dùng.

• docs/VERIFICATION.md và artifacts/SMARTTOUR-AUDIT-2026-10-18.md: nhật ký và ma trận kiểm chứng.

• docs/IMPLEMENTATION-STATUS.md, ADMIN-GUIDE.md và SMARTTOUR-DEMO.md: phạm vi triển khai và hướng dẫn.

• docs/NARRATION-CONTENT.md và DISTRICT1-SOURCES.md: nguồn nội dung, ảnh, địa chỉ và tọa độ.

• Source trọng tâm: App.jsx/api.js/offline/bundleService; controllers và services C#; PlatformService/TranslationService; JsonPlatformStore.

• Dac-ta-use-case-SmartTour-Guide: 28 Use Case với điều kiện, luồng, ngoại lệ, API và nghiệm thu.

### Nguồn thông tin địa điểm

Chợ Bến Thành: benthanhmarket.vn/about/gioi-thieu-ve-cho-ben-thanh.html

Dinh Độc Lập: dinhdoclap.gov.vn/kien-truc-dinh-doc-lap/

Bưu điện: vnpost.vn, bài công trình Bưu điện Trung tâm Thành phố Hồ Chí Minh.

Nguyễn Huệ: vietnamtourism.vn/index.php/tourism/items/2905/2

Nhà hát: csdl.vietnamtourism.gov.vn/dest/?item=381

Ảnh và tọa độ: các trang Wikimedia Commons được lưu ở từng POI; credit hiển thị trên UI.

### Lệnh kiểm tra hồi quy

dotnet build

dotnet test --no-build

npm.cmd run lint

npm.cmd test

npm.cmd run build

Chạy các lệnh npm tại ShortVideoTranslator.Frontend. Khi kiểm thử thao tác ghi/xóa, dùng kho thử và tài khoản giả lập; không ghi mật khẩu hoặc khóa dịch vụ vào nội dung báo cáo.
