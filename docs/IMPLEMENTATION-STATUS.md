# Đối chiếu chức năng HTML — cập nhật 14/10/2026

Nguồn yêu cầu: `C:/Users/Admin/Downloads/system-presentation-standalone.html`. Giữ ASP.NET Core/C#/React theo chỉ dẫn người dùng; không chuyển project sang FastAPI/MongoDB/Redis trong tài liệu. Bảng này mô tả đúng code hiện có, không coi cấu hình endpoint là bằng chứng dịch vụ bên ngoài đã chạy.

| Nhóm HTML | Đã triển khai trong đồ án | Phần chưa đầy đủ |
|---|---|---|
| Content | CRUD, nguồn/ảnh/địa chỉ, bảo vệ sửa đồng thời, catalog công khai, nearby Haversine, full/delta/ETag, tombstone 30 ngày, ID không tái sử dụng | Chưa có database phân tán; delta dùng timestamp thay collection version log |
| Content fallback | `/api/content?language=...`: bản đã tạo target → English → Vietnamese, trả contentLanguage/fallback/audioUrl đúng | Luồng nghe chính yêu cầu bản đúng ngôn ngữ; không tự đọc fallback bằng giọng sai |
| Publish gate | Chính thức: English text+MP3 đúng hash mới được công khai; tự queue 6 ngôn ngữ, English đầu tiên, sửa nguồn vô hiệu readiness | Demo miễn gate có nhãn; chưa có trình duyệt chỉnh riêng bản dịch đã duyệt |
| Audio | Cache MP3 disk, SHA-256 manifest, Range, tối đa 3 tác vụ, pause/resume/cancel/SSE, restart recovery, lưu task 14 ngày | Heartbeat 5 giây; maintenance hàng giờ; đã kiểm thử recovery trạng thái running từ file lưu và hoàn tất qua cache. Chưa kill Python giữa TTS thật |
| Admin/owner | Cookie/Bearer+refresh rotation, CSRF, 4 vai trò/32 quyền, user/role/place/menu CRUD, owner verified gate, đăng ký/đề xuất/duyệt, thông báo, audit | Đã thêm xác minh CCCD tùy chọn bằng ASP.NET Data Protection, API owner chỉ trả mask, admin cần user:read + poi:approve, audit không lưu số, hết đọc 180 ngày và dọn mỗi giờ. Không yêu cầu giấy tờ thật cho demo |
| AI advisor | Gemini adapter, usage, owner 10/ngày, admin không giới hạn, timeout/lỗi, draft cần người kiểm tra | Thiếu khóa/model để gọi AI thật; đã kiểm thử quota và provider errors bằng HTTP giả lập, sửa empty candidates →503; chưa có ProxyPal/OpenWeather |
| Localization | Dịch MyMemory thật 6 ngôn ngữ, warmup queue, on-demand, prepare-hotset API và cache UI ưu tiên 3, nearby 1.5 km | Đổi ngôn ngữ tự chuẩn bị hotset, hủy lượt cũ; nút nghe chỉ mở khi bundle UI và tối đa 3 POI bắt buộc đã cache. Khi hotset gần GPS ít hơn3 dùng số POI thực có; không bịa POI để đủ số. UI admin/owner còn tiếng Việt |
| UI i18n | Bundle tĩnh 6 ngôn ngữ, hash, API unavailable fallback rõ; tiêu đề/nút chính đổi theo ngôn ngữ | Cổng admin/owner và vài thông báo phụ chưa dịch; chưa có pipeline dịch bundle động |
| GPS | Opt-in, Haversine, throttle/debounce/reconcile/cooldown, timeout/cache accuracy, tự thuyết minh, prefetch 3/30s/500m và backoff | Chưa chạy tuyến thực địa; tọa độ OSM tham khảo, Vĩnh Khánh chưa có pin |
| PWA/offline | SW precache build assets, IDB catalog/translation/audio, checksum, photo đã xem, LRU 3 ngôn ngữ ×300 MP3, quota cleanup, ETag delta | Shell tách theo build-ID, precache cả worker MapLibre, sửa Vary: Origin khi tải module offline. Map blob+manifest kích hoạt nguyên tử; chưa unified pack POI+photo+map/audio trong một transaction |
| Maps | Map trang chủ+chi tiết, Leaflet OSM online; MapLibre/PMTiles offline và hybrid; manifest/nguồn/license/checksum/phạm vi/Range; gói Quận 1 thật 1.914.259 byte từ Protomaps build 20261013 | Đã kiểm tra tải, verify, reload và zoom offline khi cả server local tắt. Style giản lược không nhãn đường, không cần glyph/sprite; hybrid có code fallback/probe15s, chưa ngắt Internet toàn máy để thử recovery; iframe chi tiết vẫn online |
| Analytics | Consent opt-in, anonymous view/play, xóa dữ liệu thiết bị, dashboard/export/owner scope | Read models tính trực tiếp từ JSON; chưa có queue analytics worker |
| Runtime observability | Opt-in riêng mặc định tắt; gửi vị trí làm tròn ~100m, giới hạn tốc độ, bucket phiên/giờ, xem 1/6/24h với system:logs, xóa và thu hồi phiên chống request muộn, retention24h | Tests dùng tọa độ giả lập; chưa đo GPS thực địa; không trộn analytics, không thu vị trí thật của người dùng trong quá trình kiểm thử |

## Tài nguyên còn thiếu để vận hành đủ HTML

- Gói Quận 1 đã tạo local trong App_Data/maps (ignored). Tái tạo khi máy khác clone repo: `pwsh -File scripts/Prepare-MapPack.ps1`. Source/ODbL attribution: https://docs.protomaps.com/basemaps/downloads. Gói không được tự đưa lên Git.
- Khóa AI và tên model hợp lệ ở biến môi trường backend; không đặt vào VITE_*.
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
- Frontend lint sạch, 12/12 test đạt, production build thành công. Có cảnh báo kích thước chunk MapLibre (~294 KB gzip) và worker (~508 KB trước gzip); renderer được import động, assets được precache để dùng offline.
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


Chuyển phạm vi Quận1: Place.Region + public filter, migration SampleDataVersion4 giữ kho cũ/cấp ID mới/persist atomic, contentdelta removedIDs ngoài scope, cachecatalog/maps namespace district1, sáu UIbundles/index/PWA đổi tên. Năm địa điểm Quận1 có ảnh/địa chỉ/tọa độ dẫn nguồn; ba nơi ban đầu đã tạo18MP3 thật, hai nơi bổ sung được kiểm tra riêng và thử phát Việt/Anh. Chi tiết: [VERIFICATION.md](VERIFICATION.md), [DISTRICT1-SOURCES.md](DISTRICT1-SOURCES.md).
