# Kết quả kiểm tra SmartTour Guide — cập nhật 18/10/2026

## Thông báo chủ quán và gửi lại hồ sơ — 18/10/2026

- Inbox có chuông/số chưa đọc, lọc chưa đọc, URL chi tiết, trạng thái duyệt và ghi chú. Đọc GET không tự đánh dấu đã đọc. Tài khoản chưa xác minh vẫn xem được kết quả từ chối nhưng chưa được quản lý địa điểm.
- Hồ sơ bị từ chối gửi yêu cầu xem xét lại kèm ghi chú; chặn gửi trùng khi đang chờ và giữ lịch sử. Chỉ Admin duyệt mới mở quyền quản lý. Notification và các tài nguyên liên kết được kiểm tra theo người nhận; test chống đọc/đánh dấu thông báo người khác.
- `dotnet build`: 0 warning/error; `dotnet test --no-build`: **105/105**. Frontend **34/34**, lint sạch, production build thành công. Tách gói route App/Portal; chunk bản đồ vẫn có cảnh báo kích thước. Service worker vẫn chuẩn bị các chunk cho offline.
- Browser dùng tài khoản giả lập và kho riêng tại `artifacts/notification-browser-fixture`: thấy ghi chú từ chối khi chưa xác minh, mở lại URL chi tiết, đánh dấu đã đọc làm số đếm về 0; gửi lại hồ sơ qua form thành công, hiển thị chờ duyệt và ẩn form gửi trùng; sau Admin duyệt nhận thông báo mới. Không tạo tài khoản thử trong kho dữ liệu chính.

## Nội dung thuyết minh đầy đủ cho năm địa điểm — 18/10/2026

- Thay năm câu giới thiệu bằng bài Việt ba đoạn, lần lượt 906/981/962/957/968 ký tự cho Chợ Bến Thành, Dinh Độc Lập, Bưu điện, Nguyễn Huệ và Nhà hát. Lời dẫn và gợi ý quan sát do AI hỗ trợ biên soạn; dữ kiện lịch sử/kiến trúc đối chiếu nguồn cơ quan quản lý/du lịch trong NARRATION-CONTENT.md. Giữ nhãn demo; không cập nhật địa chỉ/ảnh/tọa độ hoặc thêm giá vé/giờ mở cửa.
- Có 25 bản ngoại ngữ soạn sẵn cho năm nguồn Việt chính xác. Provider SmartTourDraft, isMachineTranslation=true; UI ghi rõ AI hỗ trợ, chưa chuyên gia hiệu đính, không nhận là MyMemory hay bản Admin đã duyệt. Sửa nguồn sẽ không khớp gói mẫu; bản duyệt vẫn được ưu tiên và MyMemory vẫn xử lý nội dung khác.
- Migration v6 nâng đoạn mẫu hệ thống chưa sửa; không ghi đè nội dung thành viên/chủ quán, bản chính thức hoặc Quận4. Giữ visibility/media và ID; xóa readiness nguồn cũ, timestamp/version cập nhật một lần. Hai test mới kiểm tra toàn bộ sáu ngôn ngữ không gọi mạng, edited source không dùng bản cũ và các trường hợp bảo toàn dữ liệu/restart.
- Build backend 0 warning/error; **98/98** tests đạt. Frontend lint sạch, **32/32** tests đạt và production build thành công (cảnh báo chunk MapLibre lớn như trước). git diff --check không có lỗi whitespace; không commit/push/merge hoặc track MP3/App_Data.
- Tạo **30 MP3 mới bằng Edge-TTS thật**, năm task completed 6/6. Kiểm tra metadata đúng source-key/language/provider, byte count và SHA256; ffprobe đọc được thời lượng tất cả30. File thực và metadata trong App_Data/audio ignored. Nhật ký local artifacts/narration-content-jobs.json, narration-content-audio-check.json.
- Thời lượng Việt: Bến Thành56.352s, Dinh59.976s, Bưu điện59.880s, Nguyễn Huệ59.544s, Nhà hát59.424s. Mỗi bản khoảng một phút; không lấy MP3 một câu cũ để chứng minh bài mới.
- Browser thật: chọn đủ năm địa điểm và đọc được đúng độ dài bài mới từ API; CSS pre-line giữ ba đoạn. Bến Thành Việt56.352s và Anh50.496s, Nhà hát Việt59.424s phát với paused=false/error=null; bản Anh và Nhà hát có readyState4. Chuyển địa điểm/ngôn ngữ dừng bản trước. UI tiếngAnh hiển thị nhãn SmartTourDraft đúng; không dùng speechSynthesis. Bản ngoại ngữ vẫn cần người thông thạo hiệu đính, không coi giải mã MP3 là kiểm tra ngữ nghĩa/phát âm toàn bộ30file.
- Minh chứng: artifacts/narration-content-tested.png. API5132 và frontend5173 hoạt động; dữ liệu mới/MP3 vẫn giữ sau restart backend.
- File đợt này: Data/DemoNarrations.cs, DemoPlaceRepository.cs, JsonPlatformStore.cs; Business/TranslationService.cs; Api/Program.cs; Frontend/src/App.jsx/App.css; Tests/DemoNarrationTests.cs; docs/NARRATION-CONTENT.md, DISTRICT1-SOURCES.md, IMPLEMENTATION-STATUS.md, VERIFICATION.md.

## Startup, gom cụm bản đồ, analytics và nhãn Portal — 18/10/2026

- `dotnet build`: 0 warning/error; `dotnet test --no-build`: **96/96 đạt**. Frontend lint sạch, **32/32 tests đạt**, production build thành công. Còn cảnh báo chunk MapLibre lớn (~294 KB gzip); chưa đo tải production.
- Backend thêm3 bài cho batch atomic/consent/dedupe/heartbeat, forget xóa processed/pending/heartbeat và từ chối request muộn sau restart, phục hồi queue từ file, giới hạn batch/tuổi sự kiện. Test tự động dùng kho temp, không ghi dữ liệu thật.
- Frontend thêm9 bài so với23 trước: queue consent/batch/retry/stable ID/revocation/capacity, cluster theo zoom/selected/invalid tọa độ, dictionary Portal giữ nội dung nguồn, startup probe HTTP/JSON/network/retry/cancel. Không dùng fixture thay cho thử trình duyệt.
- Thử trình duyệt: dialog startup hiển thị5POI, Backend ready và UI+3MP3 ưu tiên ready; đổi English rồi đợi ready, Start exploring hoạt động. Không bật GPS/runtime sharing khi thử.
- Online desktop có cụm2POI, nhấn tách ghim (count0). Offline mobile390px có cụm Nguyễn Huệ/Nhà hát, nhấn tách ghim (count0). Sửa popup tự mở che cụm, callback zoom Leaflet khi đổi chế độ và đồng bộ ghim MapLibre ngay sau fitBounds. Lượt thử này dùng archive đã lưu, không tắt Internet/server để tuyên bố offline toàn máy.
- Public và Admin ở390px: innerWidth390/document scrollWidth375 (trừ thanh cuộn), không tràn trang. Khôi phục viewport sau thử; bảng/thumbnail có cuộn nội bộ.
- API thật với thiết bị/sự kiện **synthetic tự tạo**: batch lần1 queued2, gửi lại queued0; worker xử lý đúng1view+1heartbeat, pending0. Forget xóa dữ liệu probe, request muộn HTTP409. Không lấy mã thiết bị hoặc GPS thật. Tombstone của probe giữ31ngày theo chính sách.
- Admin: đăng xuất/đăng nhập tài khoản demo hiện có thành công; không thay mật khẩu. Đổi English/Japanese thấy menu/dashboard đúng; mở form sửa rồi đóng, không ghi thay đổi POI. Ghi chú/lỗi backend còn tiếng Việt, Portal có thông báo fallback; chưa coi là bản địa hóa toàn bộ Admin.
- MP3 tiếngViệt Chợ Bến Thành: sau click nghe paused=false/error=null/duration6.192s; sau phát xong readyState4/error=null. Đây là MP3 có sẵn, không chứng minh đã tạo TTS mới trong lượt này.
- Admin system-status: core ready,5POI,7 thành phần kiểm tra, worker thống kê bật, PMTiles1.914.259byte đúng checksum;6ngôn ngữ đều5/5MP3. AI/weather thiếu cấu hình, không gọi provider thật.
- Minh chứng ignored: artifacts/startup-ready-tested.png, map-clusters-tested.png, admin-localization-tested.png, completion-system-tested.png. API5132/Vite5173 đang chạy. Lượt này Vite dev; PWA/shell đầy đủ cần build+preview theo SMARTTOUR-DEMO.md.
- Files: Api/Services/AnalyticsQueue.cs, Controllers/AnalyticsController.cs/AdminController.cs/OwnerController.cs/SystemStatusController.cs, Program.cs; Business/PlatformModels.cs; Frontend/src/mapClusters.js/HomeMap.jsx/OfflineMap.jsx/App.css, analytics.js/analyticsQueue.js, StartupPanel.jsx/startupProbe.js/App.jsx/ExploreTools.jsx/TourDesign.css, PortalLocale.jsx/portalMessages.js/Portal.jsx/Portal.css; Tests/AnalyticsBatchTests.cs/AdminIntegrationTests.cs; Frontend/tests/analyticsQueue.test.js/mapClusters.test.js/portalMessages.test.js/startupProbe.test.js; docs.

Còn cần tài nguyên/xác minh: AI/OpenWeather thật, GPS thực địa, toàn bộ ghi chú Admin đa ngôn ngữ, hosting/tải lớn và kho phân tán. Giữ stack ASP.NET/C#/React theo yêu cầu. Ma trận cập nhật ở IMPLEMENTATION-STATUS.md; các mục bên dưới là lịch sử kiểm tra trước giai đoạn này.

## Đối chiếu HTML: AI Advisor + readiness — 15/10/2026

- Backend build0warning/error, **93/93** tests; frontend lint sạch, **23/23** tests và build đạt. Cảnh báo chunk MapLibre lớn còn như trước. Thêm13test backend: fallback/protocol, failed request không tính lượt, weather opt-in/cache/ownerIDOR/stale, destination không hợp lệ, reservation đồng thời, adminunlimited/restart, per-provider cancellation; readiness aliases/permission/no probe leak/catalog version, corruption503+recovery sau cache.
- Provider fixtures dùng tên/key/response synthetic; không gọi Gemini, ProxyPal hay OpenWeather thật. Không dùng test này để tuyên bố dịch vụ ngoài đã hoạt động. Source/protocol từ primarydocs trong [AI-ADVISOR.md](AI-ADVISOR.md).
- LiveAPI: usage ready=false/providers=[]/weatherReady=false; gọi enhance-description trả503, used vẫn0 và inFlight0. UI form Admin báo thiếu cấu hình, khóa nút tạo, vẫn chỉnh thuyết minh thủ công và kiểm tra lại được. Chưa thử happy path AI preview trên browser với model thật vì máy thiếu cấu hình.
- Live `/health/ready`200; Admin system-status coreReady=true,5POI,6ngôn ngữ đều5/5MP3 hiện có đúng checksum/source, bản đồ1.914.259byte verified; AI/weather “Chưa cấu hình”. Không tạo MP3 mới hay sửa POI khi kiểm thử đợt này.
- Ảnh `artifacts/admin-ai-unavailable-tested.png`, `artifacts/admin-system-status-tested.png`. API5132 và frontend5173 đang chạy. Không commit/push/merge, không in khóa bí mật.
- File: Api/Services/AiAdvisorService.cs, AiQuotaService.cs, WeatherContextService.cs, SystemReadinessService.cs; Controllers/AiController.cs, SystemStatusController.cs; Program.cs; Frontend/src/AiAdvisorPanel.jsx, Portal.jsx/CSS; Tests/AiAdvisorPipelineTests.cs, SystemReadinessTests.cs và docs.

Đối chiếu lại vẫn còn: cấu hình/test dịch vụ AI/weather thật; bản địa hóa đầy đủ Admin/owner và thông báo phụ; clustered POI layer; analytics heartbeat/batch/worker; startup splash/hotset UX chưa tương đương toàn bộ HTML; kho JSON thay database/cache phân tán theo yêu cầu giữ stack; chưa khảo sát GPS thực địa/kiểm thử tải lớn. Không coi toàn bộ HTML đã hoàn thành. Ma trận ở [IMPLEMENTATION-STATUS.md](IMPLEMENTATION-STATUS.md).

## Ảnh Nguyễn Huệ mới hơn — 15/10/2026

- Thay bìa2020 bằng ảnh26/02/2025 và gallery2017 bằng ảnh đêm04/12/2024. Ngày/tác giả/license đối chiếu Commons imageinfo+Exif và xem ảnh gốc. Nguồn/ghi chú ở [DISTRICT1-SOURCES.md](DISTRICT1-SOURCES.md). Không suy diễn ngày chỉnh trang là ngày chụp, không tuyên bố có ảnh2026.
- Download bản gốc vào artifacts ignored để kiểm tra ảnh và chuẩn bị tài nguyên website: ảnh2025=3.688.783byte; ảnh2024=2.577.037byte, JPEG hợp lệ. Upload qua POST/api/media có quyền Admin, sau đó PUT/api/admin/places/7 giữ nguyên source/audio/các địa điểm khác. Ảnh live local dưới App_Data/media ignored; tránh lỗi429 khi browser gọi trực tiếp Commons. Seed mới lưu nguồn remote gốc, không lưu URL local ngẫu nhiên vào seed.
- Browser cảhaiảnh loaded=true/naturalWidth4032 từ API local; khung cắt/tên địa điểm/ngày chụp/nguồn hiển thị đúng. Ảnh `artifacts/nguyen-hue-2025-cover-tested.png` và `artifacts/nguyen-hue-2024-night-tested.png`. Tự chuyển vẫn3giây, đã bật lại sau kiểm tra. Chưa thử lại offline toàn gói trong đợt đổi ảnh.
- Backend build0warning/error,80/80 tests. Không đổi frontendcode nên không chạy lại bộ test frontend trong đợt này; trước đó23/23 và build đạt. Restart API, ảnh/thay đổi vẫn tồn tại; cả5132/5173 hoạt động. Không commit/push/merge.
- File: Data/DemoPlaceRepository.cs, DemoPlacePhotos.cs và tài liệu nguồn/kiểm tra/hướng dẫn; live photo/catalog trong App_Data ignored.

## Đủ gallery5địa điểm, nhịp3giây — 15/10/2026

- Bổ sung Nguyễn Huệ: góc phố/đài phun nước2017, Terrazzo, CC BY2.0; Nhà hát Thành phố: ảnh đêm2019, Suicasmo, CC BY-SA4.0. Xác minh nguồn bằng Commons+imageinfo, thumbnail1280px HEAD200/image/jpeg, browser tải cả hai naturalWidth1280. Có tên địa điểm, năm/tác giả/nguồn và ghi chú cắt khung. Xem [nguồn ảnh](DISTRICT1-SOURCES.md).
- Giữ ảnh bìa và các ảnh đang có; lưu hai gallery qua Admin API có kiểm tra UpdatedAt, cập nhật DemoPlacePhotos cho lần seed mới. Cả5địa điểm live có ảnh bìa+1ảnh bổ sung. Không sửa nguồn thuyết minh hoặc audio.
- PlaceGallery đổi timer6000→3000ms và nhãn3giây/ảnh. Giữ hover/focus/visibility/reduced-motion, nút dừng/bật, trước/sau và thumbnail. Đo bằng browser waitFor ở cùng một chu kỳ: từảnh1 đếnảnh2 trong2837ms tính sau khi click ra ô tìm kiếm (timer bắt đầu trước thời điểm đo khoảng0,16giây), timeout5giây. Các lần chụp rời có thể bỏ qua khung do ảnh đã vòng lại; không dùng chúng để suy ra timer lỗi.
- `dotnet build`:0warning/error; `dotnet test --no-build`:80/80. `npm run lint`:sạch; `npm test`:23/23; `npm run build`:đạt, còn cảnh báo chunk MapLibre lớn. API5132 và frontend5173 đang chạy lại. Không commit/push/merge.
- Ảnh minh chứng: `artifacts/nguyen-hue-gallery-3s.png`, `artifacts/opera-gallery-3s.png`. Chưa thử tải lại gói offline đủ10ảnh trong đợt này; gallery bổ sung tiếp tục đi qua pipeline staging/checksum đã có kiểm thử.
- File: Frontend/src/PlaceGallery.jsx, Data/DemoPlacePhotos.cs và ba tài liệu này; live data trong App_Data ignored.

## Đổi ảnh bưu điện và slideshow — 15/10/2026

- Đổi bìa bưu điện sang ảnh không gian bên trong2014 đã xác minh. Thay ảnh bên ngoài cũ bằng mặt tiền màu vàng2019 của Balon Greyjoy, Commons CC0; thumbnail1280px tải được trong trình duyệt và có nguồn riêng. Lưu qua Admin API với UpdatedAt, giữ nguyên nội dung thuyết minh/audio, ID và các địa điểm khác. Cập nhật seed cho lần chạy mới.
- PlaceGallery tự chuyển6giây/ảnh khi gallery nằm trong vùng nhìn, trang không bị ẩn và không có hover/focus bên trong. Có nút bật/tạm dừng, trước/sau, thumbnail; một ảnh không chạy timer. Thiết bị bật prefers-reduced-motion mặc định tắt autoplay; người dùng vẫn có thể chủ động bật. Timer/observer/listener được giải phóng khi đổi địa điểm/unmount. Hiệu ứng fade chỉ bật khi không yêu cầu giảm chuyển động.
- Browser thực tế: bật tự chuyển từ ảnh1, đưa focus ra ô tìm kiếm; sau6,5giây thấyảnh2 với chú thích mặt tiền2019. Nút tiếp theo vòng2→1, nút trước1→2; tạm dừng, chờ6,5giây vẫnảnh2. Ảnh tải thành côngnaturalWidth1280, tên địa điểm rõ. Minh chứng `artifacts/post-office-slideshow-tested.png`.
- Backend build0warning/error,80/80 tests. Frontend lint sạch,23/23 tests, build đạt (cảnh báo MapLibre như trước). Các kiểm tra thời gian slideshow là thử trình duyệt thực tế, chưa có test tự động cho timer. Không commit/push/merge; API5132 và frontend5173 đang chạy.
- Files: Frontend/src/PlaceGallery.jsx, TourDesign.css; Data/DemoPlaceRepository.cs, DemoPlacePhotos.cs; docs/DISTRICT1-SOURCES.md, SMARTTOUR-DEMO.md và VERIFICATION.md. Dữ liệu live thay qua API trong App_Data ignored; không sửa trực tiếp file khi server đang dùng.

## CRUD địa điểm và thư viện ảnh — 15/10/2026

- Backend build0warning/error, **80/80** tests; frontend lint sạch, **23/23** tests và build đạt. Cảnh báo chunk MapLibre lớn giữ nguyên. Các kiểm thử cũ về phân quyền, CSRF, owner scope, nguồn/activation và xung đột cập nhật vẫn đạt.
- Thử API local thực tế bằng một địa điểm QA riêng: create200; sửa tên/chú thích ảnh thành công; gửi snapshot cũ409; bỏ ảnh thành công; ẩn trả404 ở API public; delete204; detail sau xóa404. Dọn địa điểm QA, catalog trở lại5điểm. Không sửa/xóa điểm thật khi kiểm tra CRUD.
- Hai test backend mới kiểm tra gallery roundtrip/persist, URL javascript bị400 và không lưu dở phần tên, stale edit409, hide/delete/tombstone, và migration không ghi đè gallery tùy chỉnh/không thêm lại ảnh đã bỏ. Fixture URLs example.com không được tải hay coi là ảnh thật. Hai assertion persistence cũ chuyển sang so sánh cấu trúc để bao gồm mảng ảnh sau JSON roundtrip.
- Model thêm `Place.Images`, tối đa6ảnh bổ sung, mỗi ảnh cần URL, chú thích, credit, nguồn HTTPS và focus0–100. Admin/owner editor có thêm/sửa/bỏ ảnh; owner vẫn phải gửi duyệt. API vẫn giữ ảnh bìa riêng và kiểm tra quyền cũ.
- Migration samplev5 chỉ bổ sung ảnh cho mẫu Quận1 chưa có gallery và còn dùng ảnh bìa mặc định; giữ nguyên ảnh tùy chỉnh, dữ liệu người dùng, nguồn thuyết minh, ID và bản dịch/audio. Mảng rỗng thể hiện đã chủ động bỏ ảnh, không seed lại khi restart.
- Thêm3ảnh thật có nguồn/năm: Chợ Bến Thành bên trong2008, Dinh Độc Lập từ khuôn viên2013, bưu điện bên trong2014. HEAD200/image/jpeg; browser chuyển từng ảnh thành công, naturalWidth tương ứng3264/1286/1280. Admin editor đọc đúng gallery và thêm/bỏ trường ảnh nháp được; không lưu thay đổi thử trên địa điểm thật. Xem [nguồn ảnh](DISTRICT1-SOURCES.md).
- Desktop có tên địa điểm, caption, nguồn và thumbnail; viewport390px không tràn ngang. Minh chứng `artifacts/place-gallery-tested.png`.
- Gói offline đầy đủ đã mở rộng để lưu cả gallery; có regression test lưu ảnh bìa+ảnh bổ sung. Chưa tải lại toàn bộ gói8ảnh để thử mất mạng trong đợt này. Backend5132/frontend5173 đều được chạy lại. Chưa kiểm thử tải lớn hoặc nhiều instance; JSONstore hiện phục vụ demo một tiến trình.

File giai đoạn này: Business/Place.cs, PlatformService.cs; Data/DemoPlacePhotos.cs, DemoPlaceRepository.cs, JsonPlatformStore.cs; Frontend/src/PlaceGallery.jsx, placePhotos.js, PlaceImage.jsx, App.jsx, TourDesign.css, Portal.jsx/CSS, bundleService.js; Tests/PlaceGalleryIntegrationTests.cs, PlatformServiceTests.cs; Frontend/tests/offlineBundle.test.js.

## Admin duyệt bản dịch — 15/10/2026

- `dotnet build`: thành công, 0 warning/error. `dotnet test --no-build --logger 'console;verbosity=minimal'`: **78/78**, không skip. `npm run lint`: sạch; `npm test`: **22/22**; `npm run build`: thành công. Cảnh báo chunk MapLibre lớn còn như trước.
- Thêm 8 tests backend: nháp riêng, sửa đồng thời409, publish/withdraw/persist, nguồn đổi, chặn copy nguyên tiếng Việt, giữ gate English khi thu hồi, phân quyền editor, HTTP manifest/media/cache cũ, TTS unavailable giữ nguyên bản hiện tại (một test bao gồm nhiều tình huống). Fixtures bytes=[1,2,3] và Python executable không tồn tại chỉ kiểm tra logic lỗi/ACL, **không phải TTS thật**.
- Thêm 2 tests frontend: khóa cache automatic v1/reviewed v2 không trùng nhau; bundle offline dùng đúng revision đã duyệt và giữ provider reviewed/isMachineTranslation=false.
- Kiểm tra thật qua Admin UI: lưu nháp tiếng Anh Chợ Bến Thành; POST translate của khách vẫn trả bản MyMemory cũ/provider máy. Nhấn Duyệt & tạo MP3: Edge-TTS tạo **48.384 byte**, SHA256 `14f08eda1f7519d57c8bbab5870e298f1b99904f1bc8f5841c338f0162155f68`; bản duyệt được lưu local trong App_Data (ignored).
- Trang khách chọn English: thấy nội dung bản duyệt, phát MP3 Blob **8,064s**, readyState4, paused=false, error=null. Audio ở Admin cũng tải được8,064s. API speech200/audio-mpeg,48.384byte; translate provider=reviewed/isMachineTranslation=false; catalog5địa điểm,manifestchỉaudio đang dùng; gửi revision cũ trả409; CORS chấp nhận localhost:5173.
- Khởi động lại API, reload Admin: bản dịch đã duyệt và MP3 còn nguyên. Hai server chạy lại ở5132/5173. Không đổi mật khẩu, commit, push hoặc merge; vẫn nhánhfeature/api.
- Ảnh: `artifacts/admin-translation-reviewed.png`. Giao diện có5địa điểm×5ngôn ngữ biên tập; mới duyệt và nghe thực tế bản tiếng Anh Chợ Bến Thành trong đợt này. Không tuyên bố đã rà soát chuyên môn toàn bộ30bản thuyết minh hoặc thử offline thật bản duyệt mới; offline reviewed được kiểm tra bằng fixtures.
- Thiếu để đủ HTML: khóa/model AI cho gọi thật, ProxyPal/OpenWeather, bản địa hóa Admin/owner, khảo sát GPS thực địa. Xem ma trận [IMPLEMENTATION-STATUS.md](IMPLEMENTATION-STATUS.md).

## Giao diện Quận 1 — 15/10/2026

- Đổi phần giới thiệu sang bố cục hai cột, ảnh địa điểm thật lấy từ API có ghi nguồn; giữ tông cam, kem, be và nâu nhạt. Tên địa điểm dùng font UI để hiển thị dấu tiếng Việt rõ. Không thêm ảnh stock hoặc số liệu đánh giá giả.
- Danh sách/chi tiết được đưa ngay sau phần giới thiệu; bản đồ và tiện ích GPS/offline giữ trên trang chủ, nằm phía dưới. Thêm bộ lọc danh mục từ dữ liệu API, nhãn Tất cả/Danh mục có đủ sáu ngôn ngữ.
- Kiểm tra trình duyệt ở1280×900 và390×844: không tràn ngang; bộ lọc Kiến trúc trả một nơi, tìm không dấu `nguyen hue` trả Nguyễn Huệ, xóa tìm kiếm khôi phục năm nơi. Thẻ ảnh có tên/địa chỉ; hình và nguồn cũ được giữ.
- Phát MP3 tiếng Việt Chợ Bến Thành trên mobile: duration6,192s, readyState4, pausedfalse, không lỗi media. Sửa Popup MapLibre không tự lấy focus khi mở ghim mặc định để tránh kéo trang xuống bản đồ lúc tải.
- `dotnet build`: 0 warning/0 error; `dotnet test --no-build`:70/70. `npm run lint` sạch, `npm test`:20/20, `npm run build` đạt; vẫn có cảnh báo chunk MapLibre lớn đã import động.
- API health, places, ui-bundles/en trả200; bundle có nhãn bộ lọc mới. File sửa: Frontend/src/App.jsx, TourDesign.css, ui-bundles.json, OfflineMap.jsx. Không thay dữ liệu địa điểm, tài khoản hoặc quyền.
- Minh chứng desktop: `artifacts/tour-design-desktop.png`; mobile: `artifacts/tour-design-mobile.png`. Kiểm tra viewport xong sẽ trả về kích thước mặc định của người dùng.
- Đợt này ưu tiên giao diện theo yêu cầu mới. Các chức năng HTML chưa có như chỉnh/duyệt bản dịch riêng, AI thật/ProxyPal/OpenWeather vẫn được ghi ở IMPLEMENTATION-STATUS.md; không tuyên bố đã hoàn tất các phần đó.

## Kết quả mới nhất — gói offline đầy đủ

- `dotnet build`: 0 warning/0 error; `dotnet test --no-build`: 70/70. Frontend lint sạch, 20/20 tests, production build đạt; còn cảnh báo kích thước chunk MapLibre.
- UI thật: tải/cập nhật gói tiếng Việt gồm 5 địa điểm, 5 ảnh, 5 MP3, bản đồ và giao diện, khoảng 9,6 MB. Kích hoạt trong một transaction IDB sau xác minh SHA256 MP3/map, signature/digest ảnh và kiểm tra lại content/map ETag304. Nguồn đổi, trục trặc hoặc hủy sẽ không commit gói mới.
- Tắt cả API5132 và Vite5173, reload: trang/danh sách/map vector hoạt động. Mở Nguyễn Huệ ngoài 3 POI hotset: ảnh dùng blob local, naturalWidth3984; MP3 duration6,480s, readyState4, pausedfalse, errornull. Gói ảnh đã tải đủ qua CORS, không phụ thuộc ảnh đã xem.
- Cập nhật khi hai server tắt báo Backend không kết nối; kiểm tra gói vẫn sẵn sàng 5/5/5. Chưa ngắt Internet toàn máy; iframe chi tiết vẫn online. Chưa mô phỏng quota đầy trình duyệt.
- Tám kiểm thử mới dùng fixture byte giả lập có ghi chú; không coi fixture là MP3/PMTiles thật. Kiểm tra commit sau đủ thành phần, ảnh403, thiếu/hỏng audio, thay đổi phiên bản, hủy, repair sau eviction/corruption và pin ngôn ngữ đang dùng.
- GPS dùng result.timestamp thay thời điểm callback để không coi vị trí cached là phép đo mới. Chưa đo GPS thực địa.
- Minh chứng: `artifacts/offline-complete-bundle-tested.png` (ignored). API/Vite được khởi động lại cho demo sau kiểm tra.

## Giai đoạn trước — năm địa điểm Quận 1

- Bổ sung Phố đi bộ Nguyễn Huệ (ID7) và Nhà hát Thành phố (ID8); nguồn nội dung, ảnh/giấy phép, địa chỉ và tọa độ trong [DISTRICT1-SOURCES.md](DISTRICT1-SOURCES.md).
- `dotnet build` đạt0warning/0error; `dotnet test --no-build`70/70; `npm run lint`, `npm test`12/12, `npm run build` đạt. Còn cảnh báo chunk MapLibre lớn.
- Migration SampleDataVersion4 thêm hai nơi cho kho đã có ba nơi Quận1; giữ nội dung đã biên tập và không tạo trùng sau restart. Hai detail test mới và migration regression test đã đạt.
- API và proxy Vite thật200, danh sách5địa điểm; đủ5asset mỗi ngôn ngữ vi/en/zh/ja/ko/fr. Hai tác vụ bổ sung completed6/6; đã tải/đối chiếu SHA256 toàn bộ30MP3.
- Trình duyệt: cả5ảnh có naturalWidth>0, map có5ghim; mở chi tiết hai nơi đúng ảnh/địa chỉ/nguồn. MP3Vi Nhà hát6.456s, Nguyễn Huệ6.480s, readyState4; đã thấy trạng thái phát/pausedfalse và kết thúc bình thường. Không kiểm duyệt ngữ nghĩa tất cả bản dịch máy.
- Không cần trích xuất map lần nữa: cả hai tọa độ mới nằm trong gói PMTiles Quận1 đã verify. Offline reload thử thực tế trước khi thêm hai nơi; sau bổ sung đã kiểm tra5ghim và tải asset, chưa lặp lại tắt hai server cho toàn bộ5nơi.
- Khi lượt kiểm tra bị ngắt, hai tiến trình local đã dừng; khởi động lại API5132/Vitepreview5173, thử phát lại thành công. Hai server được để chạy cho demo.
- Minh chứng: `artifacts/district1-five-places-tested.png`. Files bổ sung: Data/DemoPlaceRepository.cs, JsonPlatformStore.cs; Tests/PlaceServiceTests.cs, PlatformServiceTests.cs; Frontend/src/HomeMap.jsx; docs/DISTRICT1-SOURCES.md và tài liệu trạng thái.

## Giai đoạn chuyển sang ba địa điểm Quận 1 (trước yêu cầu bổ sung)


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
