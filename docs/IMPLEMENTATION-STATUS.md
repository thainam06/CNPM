# Đối chiếu chức năng HTML — cập nhật 18/10/2026

Cập nhật mới: inbox/chuông thông báo của chủ quán, chi tiết quyết định có URL riêng, đánh dấu đã đọc, hồ sơ từ chối gửi lại kèm ghi chú. Backend 105/105 và frontend 34/34; xem VERIFICATION.md. Hướng dẫn đầy đủ 11 mục quản trị: ADMIN-GUIDE.md. AI/thời tiết thật vẫn cần cấu hình dịch vụ.

Nguồn yêu cầu: `C:/Users/Admin/Downloads/system-presentation-standalone.html`. Giữ ASP.NET Core/C#/React theo chỉ dẫn người dùng; không chuyển project sang FastAPI/MongoDB/Redis trong tài liệu. Bảng này mô tả đúng code hiện có, không coi cấu hình endpoint là bằng chứng dịch vụ bên ngoài đã chạy.

| Nhóm HTML | Đã triển khai trong đồ án | Phần chưa đầy đủ |
|---|---|---|
| Content | CRUD, nguồn/ảnh/địa chỉ, bảo vệ sửa đồng thời, catalog công khai, nearby Haversine, full/delta/ETag, tombstone 30 ngày, ID không tái sử dụng | Chưa có database phân tán; delta dùng timestamp thay collection version log |
| Content fallback | `/api/content?language=...`: bản đã tạo target → English → Vietnamese, trả contentLanguage/fallback/audioUrl đúng | Luồng nghe chính yêu cầu bản đúng ngôn ngữ; không tự đọc fallback bằng giọng sai |
| Publish gate | Chính thức: English text+MP3 đúng hash mới được công khai; tự queue 6 ngôn ngữ, English đầu tiên, sửa nguồn vô hiệu readiness; Admin chỉnh/duyệt bản dịch với MP3 chuẩn bị trước khi công khai | Demo miễn gate có nhãn; bản dịch đã duyệt vẫn cần người có chuyên môn kiểm tra nội dung |
| Audio | Cache MP3 disk, SHA-256 manifest, Range, tối đa 3 tác vụ, pause/resume/cancel/SSE, restart recovery, lưu task 14 ngày | Heartbeat 5 giây; maintenance hàng giờ; đã kiểm thử recovery trạng thái running từ file lưu và hoàn tất qua cache. Chưa kill Python giữa TTS thật |
| Admin/owner | Cookie/Bearer+refresh rotation, CSRF, 4 vai trò/32 quyền, user/role/place/menu CRUD, owner verified gate, đăng ký/đề xuất/duyệt, thông báo, audit | Đã thêm xác minh CCCD tùy chọn bằng ASP.NET Data Protection, API owner chỉ trả mask, admin cần user:read + poi:approve, audit không lưu số, hết đọc 180 ngày và dọn mỗi giờ. Không yêu cầu giấy tờ thật cho demo |
| AI advisor | Gemini + ProxyPal OpenAI-compatible, Auto fallback, quota 10 bản nháp thành công/ngày UTC cho owner, Admin không giới hạn, reservation chống vượt hạn mức đồng thời, lỗi/hủy trước khi hoàn tất không trừ; preview nguồn/AI, chấp nhận/bỏ, hủy khi nguồn đổi; OpenWeather opt-in với tọa độ POI xác minh, freshness/cache/timeout | Máy chưa cấu hình khóa/model/gateway/weather; mới kiểm thử provider thành công/fallback/thời tiết bằng fixtures có nhãn, chưa gọi AI hoặc OpenWeather thật. Không tạo AI giả để demo |
| Readiness/kiểm tra hệ thống | `/health`, `/health/ready`, `/api/health/ready`; Admin system:logs có bảng kho dữ liệu, worker bật/tắt, bản đồ checksum, MP3 đúng source theo6ngôn ngữ, cấu hình AI/weather | Public readiness chỉ đo kho JSON và quyền ghi thư mục, cache15giây; không coi cờ cấu hình là kiểm tra kết nối provider; chưa thử hosting production |
| Localization | Dịch MyMemory thật 6 ngôn ngữ, warmup queue, on-demand, prepare-hotset API và cache UI ưu tiên 3, nearby 1.5 km | Đổi ngôn ngữ tự chuẩn bị hotset, hủy lượt cũ; nút nghe chỉ mở khi bundle UI và tối đa 3 POI bắt buộc đã cache. Khi hotset gần GPS ít hơn3 dùng số POI thực có; không bịa POI để đủ số. Admin/owner có 6 ngôn ngữ cho điều hướng, trạng thái, biểu mẫu và thao tác chung; ghi chú vận hành/AI/bản dịch và lỗi backend vẫn tiếng Việt, có nhãn fallback rõ |
| UI i18n | Bundle tĩnh 6 ngôn ngữ, hash, API unavailable fallback rõ; tiêu đề/nút chính đổi theo ngôn ngữ | Cổng admin/owner dùng dictionary 6 ngôn ngữ cho các nhãn/thao tác chung; ghi chú dài và thông báo backend chưa dịch, UI thông báo rõ. Chưa có pipeline dịch bundle động |
| Startup | Splash chọn 6 ngôn ngữ, kiểm tra catalog/UI+3 audio ưu tiên, probe readiness 2 lần ×2,5giây; trạng thái độc lập để xem dữ liệu đã lưu khi backend lỗi, retry/bỏ qua phần chuẩn bị; GPS vẫn opt-in | Snapshot/audio đã cache cho phép chạy offline; tạo bản dịch/audio mới vẫn cần Internet. Không tự xin GPS hoặc chia sẻ vị trí |
| GPS | Opt-in, Haversine, throttle/debounce/reconcile/cooldown, timeout/cache accuracy, tự thuyết minh, prefetch 3/30s/500m và backoff | Chưa chạy tuyến thực địa; năm tọa độ Quận1 có nguồn tham khảo; chưa đo cổng vào thực địa |
| PWA/offline | SW precache build assets, IDB catalog/translation/audio, checksum, photo đã xem, LRU 3 ngôn ngữ ×300 MP3, quota cleanup, ETag delta | Gói đầy đủ map/POI/photo/UI/audio kích hoạt trong một transaction IDB, ETag kiểm tra lại trước commit, checksum/repair status, hủy tải/giữ gói cũ khi lỗi; pin ngôn ngữ đang dùng khi LRU. Đã thử góiVi thật9.6MB với hai serverlocal tắt; chưa thử quota đầy hoặc mất Internet toàn máy |
| Maps | Map trang chủ+chi tiết, Leaflet OSM online; MapLibre/PMTiles offline và hybrid; manifest/nguồn/license/checksum/phạm vi/Range; gói Quận 1 thật 1.914.259 byte từ Protomaps build 20261013 | Đã kiểm tra tải, verify, reload và zoom offline khi cả server local tắt. Style giản lược không nhãn đường, không cần glyph/sprite; hybrid có code fallback/probe15s, chưa ngắt Internet toàn máy để thử recovery; iframe chi tiết vẫn online. Đã có gom cụm theo khoảng cách màn hình, chọn cụm để mở rộng, áp dụng Leaflet và MapLibre; mức đường phố tách ghim, giữ POI đang chọn riêng |
| Analytics | Consent opt-in, anonymous view/play, xóa dữ liệu thiết bị, dashboard/export/owner scope | Batch tối đa25, heartbeat60giây khi tab hiện, gửi30giây/lần, worker2giây và queue JSON phục hồi sau restart; view/play không tính heartbeat. Read models vẫn tính từ JSON cho demo một tiến trình; chưa có Redis/distributed worker |
| Runtime observability | Opt-in riêng mặc định tắt; gửi vị trí làm tròn ~100m, giới hạn tốc độ, bucket phiên/giờ, xem 1/6/24h với system:logs, xóa và thu hồi phiên chống request muộn, retention24h | Tests dùng tọa độ giả lập; chưa đo GPS thực địa; không trộn analytics, không thu vị trí thật của người dùng trong quá trình kiểm thử |

## Tài nguyên còn thiếu để vận hành đủ HTML

Cập nhật nội dung 18/10: cả năm địa điểm đã có bài thuyết minh ba đoạn thay câu giới thiệu ngắn. Có 25 bản dịch mẫu Anh/Trung/Nhật/Hàn/Pháp với provider SmartTourDraft, ghi rõ AI hỗ trợ/chưa chuyên gia hiệu đính; chỉ dùng khi nguồn Việt khớp chính xác. Đã tạo mới và xác minh checksum/thời lượng 30 MP3 cho nguồn mới. Xem [NARRATION-CONTENT.md](NARRATION-CONTENT.md). Không dùng bản đã duyệt hoặc MP3 của nguồn cũ cho nguồn mới; không thay đổi ảnh, tọa độ hay dữ liệu thành viên đã biên tập.

- Gói Quận 1 đã tạo local trong App_Data/maps (ignored). Tái tạo khi máy khác clone repo: `pwsh -File scripts/Prepare-MapPack.ps1`. Source/ODbL attribution: https://docs.protomaps.com/basemaps/downloads. Gói không được tự đưa lên Git.
- Khóa AI/model Gemini hoặc gateway ProxyPal đã hoạt động, khóa OpenWeather nếu bật ngữ cảnh thời tiết. Adapter đã có; xem [AI-ADVISOR.md](AI-ADVISOR.md). Không đặt khóa vào VITE_*.
- Bài thuyết minh lịch sử được kiểm chứng và tọa độ cổng vào/thực địa cho geofence.

## Files thay đổi theo giai đoạn

- React/giao diện: App.jsx, App.css, index.css, api.js, HomeMap.jsx, PlaceImage.jsx, i18n.js, ui-bundles.json.
- Audio: TextToSpeechService.cs, NarrationController.cs, NarrationAssetService.cs, AudioTaskWorker.cs, AudioTasksController.cs, PlatformMaintenanceWorker.cs.
- Địa điểm/quản trị: Place.cs, PlatformModels.cs, PlatformService.cs, DemoPlaceRepository.cs, JsonPlatformStore.cs, các controller Auth/Admin/Owner/Places/Media, Security/*, Portal.jsx/CSS.
- Dịch/localization: TranslationService.cs, NarrationLanguages.cs, ContentController.cs, LocalizationsController.cs, UiBundlesController.cs, localization.js.
- Offline/GPS/analytics: offline.js, cachePolicy.js, geofence.js, ExploreTools.jsx, analytics.js, AnalyticsController.cs, MapsController.cs, manifest/sw/icons, vite.config.js.
- Kiểm thử/CI: Tests/*Tests.cs, Frontend/tests/*.test.js, .github/workflows/dotnet-ci.yml, docs/*.

Không commit/push/merge; giữ nhánh feature/api. App_Data, media, secrets, artifacts, node_modules, bin/obj/models đều Git ignore.

## Kiểm thử cập nhật 14/10/2026

- Backend build: 0 warning/0 error; 70/70 test đạt.
- Frontend lint sạch, 20/20 test đạt, production build thành công. Có cảnh báo kích thước chunk MapLibre (~294 KB gzip) và worker (~508 KB trước gzip); renderer được import động, assets được precache để dùng offline.
- CLI `pmtiles verify` đạt; archive có 30 tile entries, khung xem quanh ba điểm Quận 1, zoom0–15. SHA256 `f7eae506d4fb67027ef8fe9a6ed4435d67f909308efecdc1aba2dad502b7fef9`.
- Kiểm thử HTTP: manifest checksum, 206 Range, stale version, path traversal, corrupt archive; runtime consent/rounding/rate/revoke/permissions; identity encryption/tamper/expiry/mask/IDOR; AI quota/provider errors; audio controls/persistence/restart.
- Minh chứng trình duyệt: `artifacts/district1-offline-tested.png`; map và zoom hoạt động sau reload khi API và Vite preview đều tắt. Không dùng thử này để tuyên bố đã ngắt Internet toàn máy.
- Test AI/cache recovery dùng fixtures có nhãn; không coi response giả lập hoặc bytes cache giả lập là AI/TTS thật.

Các file giai đoạn này: Services/MapPackService.cs, OwnerIdentityService.cs; Controllers/MapsController.cs, RuntimeController.cs, OwnerIdentityController.cs, AiController.cs; PlatformModels.cs, PlatformService.cs, PlatformMaintenanceWorker.cs, Program.cs; Frontend/src/mapPack.js, OfflineMap.jsx, HomeMap.jsx, runtimePolicy.js, ExploreTools.jsx, Portal.jsx, App.css, api.js; public/sw.js, vite.config.js; Tests/MapsIntegrationTests.cs, RuntimeIntegrationTests.cs, OwnerIdentityTests.cs, AiIntegrationTests.cs, AudioTaskIntegrationTests.cs; scripts/Prepare-MapPack.ps1 và docs.

Thông tin xác minh dùng Data Protection của .NET thay Fernet theo stack C#. Trên Windows có user profile, key ring mặc định ở LOCALAPPDATA/ASP.NET/DataProtection-Keys và được DPAPI bảo vệ. Không tự chuyển key ring vào repo; triển khai trên máy/container khác cần cấu hình key persistence/encryption phù hợp: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/default-settings.

## TTS thật và readiness cập nhật cuối

- Phát hiện Edge-TTS 7.2.8 trả WSS HTTP403 khi tạo MP3 mới ngày 14/10; cập nhật Python 3.14 lên edge-tts 7.3.1 và pin trong `ShortVideoTranslator.Api/requirements-tts.txt`.
- Probe nguồn mới: vi HTTP200,22.320 byte; en HTTP200,14.976 byte. Không dùng cache làm bằng chứng cho probe này.
- Tạo 3 tác vụ Admin cho POI1,2,3 với en/vi/zh/ja/ko/fr: cả3 completed,6/6, không lỗi; đủ 18 audio của 3 địa điểm (tiếng Việt có cache cũ hợp lệ,15 audio ngoại ngữ được tạo qua pipeline). Lịch sử task lỗi7.2.8 được giữ và UI thêm timestamp.
- Trình duyệt phát bản tiếng Anh Bến Nhà Rồng: duration13.752s,paused=false,readyState4,error=null; nội dung thật do MyMemory dịch từ nguồn Việt. Ảnh `artifacts/narration-english-tested.png`.
- Readiness tự động kiểm tra trên UI tiếng Hàn: trạng thái “Ngôn ngữ ko sẵn sàng: giao diện và 3/3 thuyết minh ưu tiên đã lưu”; nội dung Hangul hiện đúng lane dịch. Chọn ngôn ngữ tiếp sẽ abort hotset cũ.
- Backend cache audio kiểm tra SHA256, metadata/language/text và kích thước; cache hỏng không được quảng bá ready, có thể tạo lại khi có mạng.
- Thêm 3 regression tests Service Worker: VaryOrigin module/worker, HTML không lẫn build, không cache API/admin/owner. Tổng frontend 12 tests, backend 65 tests.
- Minh chứng hàng đợi thật: `artifacts/admin-audio-six-languages.png`. Không kiểm thử bằng giấy tờ hoặc GPS thật của người dùng.

Manifest bản đồ có fingerprint SHA256 của toàn bộ descriptor (version/source/license/bounds/bytes/archive checksum), nên đổi metadata dù bytes PMTiles giữ nguyên vẫn thay ETag; có regression test cho trường hợp này.


Chuyển phạm vi Quận1: Place.Region + public filter, migration SampleDataVersion4 giữ kho cũ/cấp ID mới/persist atomic, contentdelta removedIDs ngoài scope, cachecatalog/maps namespace district1, sáu UIbundles/index/PWA đổi tên. Năm địa điểm Quận1 có ảnh/địa chỉ/tọa độ dẫn nguồn; đã tạo và xác minh30MP3 thật, hai nơi bổ sung đã thử phát Việt trên trình duyệt và thử phát Việt/Anh. Chi tiết: [VERIFICATION.md](VERIFICATION.md), [DISTRICT1-SOURCES.md](DISTRICT1-SOURCES.md).


## Giai đoạn gói offline đầy đủ (14/10/2026)

- Gói tiếng Việt thật lưu 5 địa điểm, 5 ảnh, 5 MP3, bản đồ và giao diện, khoảng 9,6 MB. Tắt API/Vite rồi reload: bản đồ vector hoạt động, ảnh Nguyễn Huệ dùng blob local (3984px), MP3 có duration 6,480s / readyState 4 / paused false. Cập nhật khi mất mạng báo lỗi và gói cũ vẫn sẵn sàng.
- Frontend: 20/20 kiểm thử, lint sạch, build đạt. Backend: 70/70 kiểm thử, build không warning/error. Tám bài mới kiểm tra: chỉ commit khi staging hoàn tất, MP3 hỏng checksum, phiên bản đổi, thiếu audio, nguồn ảnh trả403, hủy, reconcile sau eviction/corruption và giữ ngôn ngữ đang dùng khi LRU.
- Hủy/checksum/phiên bản lỗi dùng fixture có nhãn; chưa giả lập thiết bị đầy dung lượng. Ảnh tải trực tiếp qua HTTPS/CORS; digest ảnh được tính từ byte đã nhận để phát hiện hỏng local, không phải checksum do nguồn công bố. Cần tải lại để kiểm tra ảnh nguồn đã đổi.
- Files: Frontend/src/bundleService.js, OfflineBundleTools.jsx, offline.js, cachePolicy.js, ExploreTools.jsx, PlaceImage.jsx, App.jsx/App.css, HomeMap.jsx, mapPack.js; tests/offlineBundle.test.js/cachePolicy.test.js.
- Admin chưa đủ toàn bộ HTML: AI/ProxyPal/OpenWeather thật còn thiếu cấu hình dịch vụ; adapter đã bổ sung ở giai đoạn15/10. Bản địa hóa Admin/owner còn thiếu. Chỉnh/duyệt bản dịch đã có. Không coi endpoint có code là bằng chứng dịch vụ ngoài đã chạy.


## Cải thiện giao diện — 15/10/2026

Giao diện công khai có ảnh hero lấy từ API, bố cục cam–kem–be, danh sách/chi tiết nằm trước bản đồ và tiện ích offline, lọc theo danh mục với nhãn sáu ngôn ngữ. Đã kiểm tra desktop/mobile, tìm kiếm, bộ lọc và phát MP3; giữ nguyên Admin/owner, dữ liệu và quyền. Chi tiết file và kết quả ở [VERIFICATION.md](VERIFICATION.md). Những chức năng HTML còn thiếu phía trên vẫn chưa được coi là hoàn thành.

## Biên tập và duyệt bản dịch — 15/10/2026

- Admin → **Bản dịch thuyết minh**: chọn năm địa điểm Quận 1, chọn en/zh/ja/ko/fr, xem nguồn Việt, lấy bản dịch hiện tại để sửa, lưu nháp, duyệt & tạo MP3, nghe bản đang dùng, thu hồi. Nguồn Việt sửa ở Địa điểm.
- Đọc/lưu cần `poi:update`; duyệt/thu hồi cần thêm cả `content:moderate` và `content:publish`. CSRF, kiểm tra lại quyền sau TTS, optimistic version/source hash và audit áp dụng. Bản nháp không đổi nội dung công khai; nguồn thay đổi làm bản duyệt cũ mất hiệu lực.
- Chỉ chuyển phiên bản công khai sau khi Edge-TTS tạo MP3 thành công. TTS lỗi trả503 và giữ bản cũ. Bản duyệt bỏ qua MyMemory; provider=`reviewed`, isMachineTranslation=false. Áp dụng cho cặp nội dung Việt chính xác/ngôn ngữ; hai địa điểm dùng chung nguồn sẽ dùng chung bản duyệt.
- Cache tự động giữ khóa SHA256 v1 cũ; bản duyệt dùng SHA256 `v2:{language}:{revision}:{source}`. Catalog/delta/ETag, RPC translate/speech, manifest, hotset và gói offline sử dụng revision. Client giữ catalog cũ nhận409 và cần tải lại. Thu hồi không xóa MP3 hay gói offline đã tải; request công khai mới chỉ thấy phiên bản đang dùng. Bản tiếng Anh duy nhất của địa điểm chính thức đang mở không được thu hồi nếu chưa có MP3 tự động thay thế hoặc chưa ẩn địa điểm.
- Backend `dotnet build`:0 warning/error; `dotnet test --no-build`:78/78. Frontend lint sạch,22/22 tests, build đạt; cảnh báo chunk MapLibre lớn còn như trước.
- Thử thật: lưu nháp tiếng Anh Chợ Bến Thành, API khách vẫn trả bản máy cũ; duyệt tạo MP3 Edge-TTS mới; trang khách hiện bản duyệt, phát8,064s, readyState4/paused=false/error=null. Chi tiết và ảnh ở [VERIFICATION.md](VERIFICATION.md). Tests tự động có byte fixtures được ghi rõ, không coi là MP3 thật.

File chính: Business/TranslationReviewService.cs, PlatformModels.cs, Place.cs, PlatformService.cs; Data/JsonPlatformStore.cs; Api/Controllers/TranslationReviewsController.cs, NarrationController.cs, Services/NarrationAssetService.cs, Program.cs; Frontend/src/TranslationPanel.jsx, Portal.jsx/CSS, narrationKey.js, api.js, offline.js, localization.js, bundleService.js, App.jsx; Tests/TranslationReviewTests.cs, TranslationReviewIntegrationTests.cs; Frontend/tests/narrationKey.test.js, offlineBundle.test.js.

## Hoàn thiện bổ sung — 18/10/2026

- Maps: `mapClusters.js`, `HomeMap.jsx`, `OfflineMap.jsx`, App.css. Cụm có số lượng, tên POI trong tooltip và nút mở bằng bàn phím; không đặt ghim cho tọa độ chưa xác minh/không hợp lệ. Bỏ popup tự mở che cụm. MapLibre đồng bộ lại ghim ngay sau fitBounds; Leaflet tắt zoom animation để tránh callback muộn khi đổi chế độ. Không thêm thư viện.
- Analytics: API `/api/analytics/batch`, giữ endpoint đơn cũ; `AnalyticsQueue.cs` và worker lưu queue JSON nguyên tử, tối đa1000 chờ/100 xử lý mỗi lượt, từ chối batch sai toàn bộ, UUID chống trùng trong tập dữ liệu đang giữ, sự kiện quá24giờ bị từ chối. Lưu sự kiện tối đa30ngày/10000 bản ghi, heartbeat không tăng view/play, dashboard có thiết bị hoạt động5phút và độ dài queue.
- Client analytics: chỉ gửi khi consent; queue local tối đa100, batch25, heartbeat60giây ở tab hiện, flush30giây, lỗi tạm giữ nguyên ID và backoff60giây; không gửi GPS/tài khoản. Tắt consent xóa queue, đổi phiên khi đồng ý lại, lưu yêu cầu forget để gửi lại sau mất mạng. Server xóa processed/pending/heartbeat, thu hồi mã phiên31ngày để chặn request muộn. Không gọi đây là hệ thống thống kê phân tán.
- Startup: `StartupPanel.jsx`, `startupProbe.js`, App/ExploreTools, TourDesign.css. Dialog native giữ focus, Escape/browse vẫn cho xem trước; chuẩn bị UI và top3 theo ngôn ngữ không giả vờ ready khi lỗi. Readiness probe tối đa2lần×2,5giây, không thay thế thử TTS mới.
- Portal: `PortalLocale.jsx`, `portalMessages.js`, Portal.jsx/CSS: 6 ngôn ngữ cho điều hướng/đăng nhập/trạng thái/các nhãn biểu mẫu và thao tác chung, lựa chọn lưu riêng ngôn ngữ thuyết minh; không dịch dữ liệu nhập hay mã quyền. Các hướng dẫn vận hành dài, AI/review và lỗi backend giữ tiếng Việt, có thông báo fallback trong ngôn ngữ đã chọn. Chưa coi là bản địa hóa toàn bộ Admin.
- Kiểm thử cụ thể và giới hạn ở VERIFICATION.md. AI/OpenWeather vẫn thiếu khóa/model/gateway thật; chưa xác minh kết nối bên ngoài. GPS chưa khảo sát tuyến thực địa; không tự tạo dữ kiện lịch sử hoặc vị trí.
