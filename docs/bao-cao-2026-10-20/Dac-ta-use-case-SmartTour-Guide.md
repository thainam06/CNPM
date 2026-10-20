# Đặc tả Use Case SmartTour Guide

Ngày 20/10/2026. Đề tài thuyết minh tự động đa ngôn ngữ Quận 1. Môn Công nghệ phần mềm.
Trường lớp, giảng viên, thành viên và MSSV: để trống để nhóm bổ sung.

## Phạm vi
Đặc tả chức năng đã triển khai; không coi AI chưa cấu hình, GPS chưa đi thực địa hay fixture là thành công dịch vụ thật.

## UC01 Khởi động và tìm kiếm địa điểm

**Tác nhân:** Khách tham quan

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Frontend và API đang chạy hoặc thiết bị đã lưu dữ liệu offline.

**Kích hoạt:** Khách mở trang chủ và nhập từ khóa.

**Điều kiện sau:** Danh sách địa điểm phù hợp được hiển thị; chưa gửi vị trí khi chưa đồng ý.

### Luồng chính

1. Khách mở trang chủ; hệ thống hiển thị hộp thoại chuẩn bị và lựa chọn ngôn ngữ.
2. Hệ thống kiểm tra readiness, tải catalog và chuẩn bị giao diện cùng các MP3 ưu tiên.
3. Khách chọn bắt đầu khám phá hoặc xem địa điểm trước khi chuẩn bị xong.
4. Khách nhập tên hoặc danh mục; hệ thống chuẩn hóa chữ thường và dấu tiếng Việt để lọc.
5. Khách chọn bộ lọc danh mục hoặc xóa bộ lọc để xem toàn bộ danh sách.

### Luồng thay thế và ngoại lệ

- A1 Không có kết quả: hiển thị danh sách rỗng có thông báo; khách sửa từ khóa.
- E1 Mất kết nối: dùng catalog đã lưu nếu có; nếu chưa có, hiển thị lỗi và nút thử lại.

**Quy tắc và giới hạn:** Catalog công khai chỉ thuộc Quận 1 và đang hoạt động; không tạo danh sách cố định tại React. GPS là tùy chọn.

**API và thành phần:** GET /api/content; GET /api/health/ready

**Tiêu chí nghiệm thu:** Tìm “nguyen hue” trả đúng địa điểm; lỗi mạng không được hiển thị như danh sách thành công rỗng.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC02 Xem chi tiết địa điểm và bộ ảnh

**Tác nhân:** Khách tham quan

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Danh sách đã tải; địa điểm còn công khai.

**Kích hoạt:** Khách chọn thẻ địa điểm hoặc ghim bản đồ.

**Điều kiện sau:** Hiển thị nội dung, địa chỉ, nguồn và ảnh của đúng địa điểm.

### Luồng chính

1. Khách chọn địa điểm; hệ thống dừng audio và hủy yêu cầu của địa điểm trước.
2. React lấy chi tiết từ API, hiển thị trạng thái đang tải.
3. Hệ thống hiển thị tên, phân loại, nội dung Việt, nhãn demo và địa chỉ có nguồn.
4. Khách xem bộ ảnh, chọn ảnh trước hoặc tiếp theo, hoặc bật và tạm dừng tự chuyển.
5. Hệ thống tải thực đơn nếu có; khách có thể mở liên kết nguồn và bản đồ chi tiết.

### Luồng thay thế và ngoại lệ

- A1 Chỉ có một ảnh: không cần điều khiển nhiều ảnh.
- E1 ID không tồn tại trả 404; ảnh lỗi có hình thay thế. Hiện lỗi tải menu bị bỏ qua, chưa có thông báo riêng.

**Quy tắc và giới hạn:** Ảnh có credit và nguồn; tự chuyển mỗi 3 giây khi đủ điều kiện hiển thị, dừng khi hover hoặc focus. Không hứa dữ liệu demo là thông tin chính thức.

**API và thành phần:** GET /api/places/{id}; GET /api/places/{id}/menu

**Tiêu chí nghiệm thu:** Chuyển nhanh giữa hai điểm không hiển thị bài cũ; kiểm tra tên và nguồn ảnh tương ứng.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC03 Khám phá trên bản đồ

**Tác nhân:** Khách tham quan

**Trạng thái:** Đã triển khai online và offline; hybrid chưa kiểm chứng đầy đủ hai chiều

**Điều kiện trước:** Có địa điểm với tọa độ đã xác minh; online cần Internet, offline cần pack.

**Kích hoạt:** Khách chọn chế độ bản đồ hoặc nhấn ghim.

**Điều kiện sau:** Địa điểm được chọn đồng bộ với phần chi tiết.

### Luồng chính

1. Hệ thống chỉ đặt ghim cho địa điểm có tọa độ hợp lệ và được xác minh.
2. Khách chọn online, offline hoặc hybrid.
3. Ở online, Leaflet hiển thị nền OpenStreetMap; ở offline, MapLibre đọc PMTiles đã lưu.
4. Khách nhấn cụm để phóng to hoặc chọn một ghim để xem địa điểm.
5. Hệ thống chuyển lựa chọn sang chi tiết; khách có thể xem toàn khu vực.

### Luồng thay thế và ngoại lệ

- E1 Chưa tải pack: hướng dẫn tải khi có mạng, không báo offline sẵn sàng.
- E2 WebGL hoặc tile lỗi: hiển thị thông báo; hybrid có cơ chế chuyển sang pack khi khả dụng.

**Quy tắc và giới hạn:** Khung bản đồ không phải ranh giới hành chính; tọa độ tham khảo không phải cổng vào đã đo. Iframe trong chi tiết vẫn cần Internet.

**API và thành phần:** GET /api/maps/offline-options; GET /api/maps/pack-manifest; GET /api/maps/files/{name}

**Tiêu chí nghiệm thu:** Ghim không xác minh không xuất hiện; bản đồ offline đọc được sau reload khi preview tắt.

**Bằng chứng:** Logic bản đồ có test; kiểm tra browser offline ngày 18/10/2026

## UC04 Chọn ngôn ngữ và xem bản dịch

**Tác nhân:** Khách tham quan

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Có nguồn Việt hợp lệ và ngôn ngữ được hỗ trợ.

**Kích hoạt:** Khách chọn một trong sáu ngôn ngữ.

**Điều kiện sau:** Nội dung đúng ngôn ngữ xuất hiện hoặc có lỗi rõ ràng, không trả bản dịch giả.

### Luồng chính

1. Khách chọn vi, en, zh, ja, ko hoặc fr; hệ thống dừng audio và hủy yêu cầu trước.
2. Hệ thống chuẩn bị giao diện cùng tối đa ba địa điểm ưu tiên.
3. React gửi nguồn tiếng Việt và revision đến API dịch.
4. Backend ưu tiên bản đã duyệt; nếu không có thì dùng bản mẫu khớp nguồn hoặc MyMemory.
5. React hiển thị nội dung, ngôn ngữ và ghi chú nguồn dịch; readiness chỉ đạt khi đủ dữ liệu yêu cầu.

### Luồng thay thế và ngoại lệ

- A1 Tiếng Việt: trả nguồn gốc, không gọi dịch vụ dịch.
- E1 Dịch vụ tắt, hết quota, timeout hoặc không dịch được: trả lỗi, cho thử lại. E2 Revision cũ trả 409.

**Quy tắc và giới hạn:** SmartTourDraft là bản ngoại ngữ soạn sẵn khớp chính xác nguồn Việt, không phải MyMemory trực tiếp; không coi nội dung Việt đọc giọng Anh là bản dịch.

**API và thành phần:** POST /api/narration/translate; POST /api/localizations/{id}

**Tiêu chí nghiệm thu:** Bản dịch khác nguồn Việt; chuyển ngôn ngữ không nhận kết quả cũ; lỗi provider không được cache thành thành công.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC05 Nghe dừng và phát lại thuyết minh

**Tác nhân:** Khách tham quan

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Địa điểm và bản dịch đã sẵn sàng; trình duyệt cho phép phát audio.

**Kích hoạt:** Khách nhấn Nghe thuyết minh.

**Điều kiện sau:** MP3 đúng nội dung được phát; yêu cầu và object URL được giải phóng khi không dùng.

### Luồng chính

1. Khách nhấn nghe; React ưu tiên MP3 đúng key đã lưu trên thiết bị.
2. Nếu chưa có, React gửi nguồn Việt, ngôn ngữ và revision cho API C#.
3. Backend lấy hoặc dịch nội dung đúng ngôn ngữ, kiểm tra cache và gọi Edge-TTS khi cần.
4. API trả audio/mpeg; React kiểm tra blob, tạo object URL và yêu cầu phát.
5. Khách nhấn dừng hoặc hủy khi đang tạo; có thể phát lại từ đầu bằng nút hoặc trình phát.
6. Khi đổi địa điểm/ngôn ngữ, hệ thống hủy request cũ và thu hồi URL không còn dùng.

### Luồng thay thế và ngoại lệ

- E1 Autoplay bị chặn: hướng dẫn nhấn phát trên trình phát MP3.
- E2 Dịch/TTS lỗi trả 503; revision cũ trả 409; audio không giải mã được có thông báo thử lại.

**Quy tắc và giới hạn:** Nguồn tối đa 3000 ký tự; giọng Việt vi-VN-HoaiMyNeural; không dùng speechSynthesis làm luồng chính. Include UC04 ở mức chuẩn bị nội dung, có thể đáp ứng bằng cache.

**API và thành phần:** POST /api/narration/speech; GET /api/narration/audio/{key}

**Tiêu chí nghiệm thu:** MP3 thực có codec/thời lượng; nghe, dừng, đổi địa điểm, phát lại không phát chồng hoặc giữ nguồn cũ.

**Bằng chứng:** Browser và TTS thật ngày 18/10; build và test hồi quy ngày 20/10/2026

## UC06 Tải gói khám phá offline đầy đủ

**Tác nhân:** Khách tham quan

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Bản production đã có service worker; đủ MP3 đúng ngôn ngữ; có Internet và dung lượng.

**Kích hoạt:** Khách nhấn Tải gói offline đầy đủ.

**Điều kiện sau:** Gói chỉ được kích hoạt khi tất cả phần đã tải và xác minh.

### Luồng chính

1. Hệ thống lấy snapshot catalog, UI bundle và manifest bản đồ hiện tại.
2. Hệ thống tải và kiểm tra checksum PMTiles, sau đó tải các ảnh địa điểm.
3. Hệ thống đối chiếu manifest audio với key của đúng nguồn và ngôn ngữ.
4. Từng MP3 được xác minh kích thước và SHA-256; tiến độ hiển thị cho khách.
5. Hệ thống kiểm tra lại phiên bản catalog và bản đồ trước khi ghi.
6. Hệ thống commit gói trong transaction IndexedDB rồi cập nhật trạng thái đã lưu.

### Luồng thay thế và ngoại lệ

- E1 Thiếu MP3, ảnh bị chặn CORS hoặc checksum sai: không thay gói cũ.
- E2 Hủy tải, đổi phiên bản hoặc hết dung lượng: báo nguyên nhân; gói cũ được giữ nếu còn nguyên dữ liệu.

**Quy tắc và giới hạn:** Bao gồm catalog, UI, map, ảnh và MP3; bản dev không coi là shell offline sẵn sàng. Runtime và pack chưa tách lưu trữ hoàn toàn.

**API và thành phần:** GET /api/content; GET /api/ui-bundles/{locale}; GET /api/maps/pack-manifest; GET /api/narration/pack-manifest

**Tiêu chí nghiệm thu:** Gói mới chỉ active sau staging; kiểm thử corrupt, abort, version change và reload offline.

**Bằng chứng:** Test tự động ngày 20/10; gói Việt 13,7 MB với 5 POI và 10 ảnh đã thử ngày 18/10/2026

## UC07 Kiểm tra cập nhật và xóa dữ liệu offline

**Tác nhân:** Khách tham quan

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Thiết bị có hoặc từng có dữ liệu offline.

**Kích hoạt:** Khách chọn kiểm tra, cập nhật, sửa gói hoặc xóa.

**Điều kiện sau:** Trạng thái gói phản ánh dữ liệu thực còn trên thiết bị.

### Luồng chính

1. Hệ thống đọc metadata gói và kiểm tra catalog, UI, ảnh, map và MP3 đã lưu.
2. Hệ thống phân loại missing, ready hoặc repair và liệt kê phần thiếu.
3. Khách chọn cập nhật/sửa; hệ thống tải gói mới theo UC06.
4. Khách có thể xóa toàn bộ dữ liệu offline hoặc chỉ xóa map pack.
5. Hệ thống cập nhật giao diện, readiness và chế độ bản đồ phù hợp.

### Luồng thay thế và ngoại lệ

- E1 Mạng lỗi khi cập nhật: báo lỗi và giữ phần dữ liệu cũ còn dùng được.
- E2 Browser đã eviction dữ liệu hoặc checksum sai: đánh dấu repair, không giữ nhãn ready giả.

**Quy tắc và giới hạn:** Audio LRU tối đa 3 ngôn ngữ và 300 file/ngôn ngữ; ưu tiên giữ ngôn ngữ đang dùng. Chưa kiểm thử disk-full trên thiết bị thật.

**API và thành phần:** IndexedDB; GET /api/content và manifest tương ứng khi cập nhật

**Tiêu chí nghiệm thu:** Xóa map chuyển về online; thiếu hoặc hỏng một phần phải nhận diện đúng, không báo hoàn tất.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC08 Định vị và tự thuyết minh khi vào vùng

**Tác nhân:** Khách tham quan

**Trạng thái:** Đã triển khai có giới hạn; chưa kiểm chứng thực địa

**Điều kiện trước:** Trình duyệt hỗ trợ geolocation; khách tự bật định vị; POI có tọa độ xác minh.

**Kích hoạt:** Khách bật định vị và đi vào vùng của POI.

**Điều kiện sau:** Địa điểm đủ điều kiện được chọn và yêu cầu phát thuyết minh.

### Luồng chính

1. Khách bật GPS và quyết định cấp quyền cho trình duyệt.
2. Hệ thống nhận vị trí đủ mới, sai số phù hợp; throttle cập nhật 5 giây.
3. Engine tính khoảng cách, kiểm tra bán kính mặc định 30 m và debounce 3 giây.
4. Hệ thống sắp xếp ứng viên theo ưu tiên rồi khoảng cách và chọn địa điểm.
5. Luồng chi tiết và audio chuẩn bị nội dung rồi phát khi trình duyệt cho phép.
6. Khi ra khỏi vùng, cooldown 5 phút được áp dụng; khách có thể tắt GPS.

### Luồng thay thế và ngoại lệ

- E1 Từ chối GPS hoặc vị trí quá cũ/sai số lớn: không tự kích hoạt, vẫn chọn thủ công được.
- E2 Hai vùng chồng lấn có thể chuyển sang bài ưu tiên thấp ở lần reconcile sau; đây là lỗi chưa sửa.

**Quy tắc và giới hạn:** GPS tại máy không đồng nghĩa đồng ý gửi vị trí. Chưa có hàng đợi audio single-slot hoàn chỉnh.

**API và thành phần:** navigator.geolocation.watchPosition; geofence.js; UC04 và UC05

**Tiêu chí nghiệm thu:** Dùng tọa độ giả lập kiểm debounce/cooldown; chưa khẳng định đi bộ ngoài thực địa.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC09 Đồng ý hoặc thu hồi thống kê ẩn danh

**Tác nhân:** Khách tham quan

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Trang khám phá đã mở; không cần tài khoản.

**Kích hoạt:** Khách thay đổi lựa chọn thống kê.

**Điều kiện sau:** Chỉ gửi sự kiện khi đồng ý; thu hồi dừng gửi và yêu cầu xóa phiên.

### Luồng chính

1. Khách bật cho phép thống kê; hệ thống tạo định danh thiết bị ẩn danh.
2. Lượt xem/nghe và heartbeat được đưa vào hàng đợi cục bộ.
3. Client gửi batch khi có mạng, tab hiện và vẫn có consent.
4. Backend kiểm tra dữ liệu, chống trùng và lưu queue để worker xử lý.
5. Khách tắt thống kê; client xóa queue, dừng gửi và yêu cầu server quên phiên.

### Luồng thay thế và ngoại lệ

- E1 Mạng lỗi: giữ ID sự kiện để gửi lại, không nhân đôi kết quả.
- E2 Yêu cầu xóa chưa gửi được: lưu để thử lại; request muộn của phiên thu hồi bị chặn.

**Quy tắc và giới hạn:** Không gửi GPS, tài khoản hoặc free text; batch tối đa 25, heartbeat không tăng view/play. Thống kê hiện dùng JSON một tiến trình.

**API và thành phần:** POST /api/analytics/batch; POST /api/analytics/forget

**Tiêu chí nghiệm thu:** Consent=false không ingest; batch retry không tăng trùng; forget xóa cả pending và processed.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC10 Chia sẻ và xóa vị trí giám sát demo

**Tác nhân:** Khách tham quan

**Trạng thái:** Đã triển khai và test bằng tọa độ giả lập

**Điều kiện trước:** GPS đang bật, có vị trí mới; khách đồng ý riêng việc chia sẻ.

**Kích hoạt:** Khách bật hoặc tắt chia sẻ vị trí với Admin.

**Điều kiện sau:** Vị trí chỉ gửi theo consent riêng; khi thu hồi có yêu cầu xóa và chặn phiên cũ.

### Luồng chính

1. Khách đọc mục đích và bật consent chia sẻ; hệ thống tạo mã phiên riêng.
2. Client kiểm tra GPS, độ mới, sai số và làm tròn tọa độ trước khi gửi.
3. Backend kiểm tra consent, tốc độ và lưu bản ghi phiên theo giờ.
4. Khách tắt chia sẻ hoặc nhấn xóa vị trí đã chia sẻ.
5. Hệ thống dừng request đang chạy, yêu cầu xóa dữ liệu và thu hồi phiên.

### Luồng thay thế và ngoại lệ

- E1 Thiếu consent hoặc tọa độ không hợp lệ bị từ chối.
- E2 Mất mạng khi xóa: báo chưa xóa được server; dữ liệu có retention 24 giờ.

**Quy tắc và giới hạn:** Mặc định tắt khi mở trang; khoảng làm tròn 100 m; tách hoàn toàn khỏi thống kê lượt nghe.

**API và thành phần:** POST /api/runtime/location; POST /api/runtime/forget

**Tiêu chí nghiệm thu:** Không trộn analytics; vị trí không đủ chất lượng không gửi; phiên bị thu hồi không ingest lại.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC11 Đăng nhập làm mới phiên và đăng xuất

**Tác nhân:** Admin hoặc chủ quán

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Có tài khoản hoạt động và thông tin đăng nhập hợp lệ.

**Kích hoạt:** Người dùng mở /admin hoặc /owner và đăng nhập.

**Điều kiện sau:** Phiên có quyền tương ứng được thiết lập hoặc bị từ chối rõ ràng.

### Luồng chính

1. Người dùng nhập username và mật khẩu.
2. Client lấy CSRF token rồi gửi yêu cầu đăng nhập.
3. Backend kiểm tra mật khẩu đã hash và trạng thái enabled; thiết lập cookie HttpOnly.
4. Client lấy thông tin quyền và chỉ hiển thị chức năng phù hợp; mật khẩu ban đầu phải đổi nếu được yêu cầu.
5. Khi access hết hạn, client thử refresh; khi đăng xuất backend thu hồi refresh của phiên và xóa cookie.

### Luồng thay thế và ngoại lệ

- E1 Sai thông tin trả 401; khóa hoặc thiếu quyền không được vào nghiệp vụ.
- E2 Refresh hết hạn/đã xoay vòng hoặc security stamp đổi: yêu cầu đăng nhập lại.

**Quy tắc và giới hạn:** Access 30 phút, refresh 7 ngày; mutation cookie cần CSRF; Bearer có lane API riêng. Đăng nhập là tiền điều kiện cho UC được bảo vệ, không bắt đăng nhập lại mỗi thao tác.

**API và thành phần:** GET /api/auth/csrf; POST /api/auth/login; GET /api/auth/me; POST /api/auth/refresh; POST /api/auth/logout

**Tiêu chí nghiệm thu:** Kiểm cookie flags, CSRF, token rotation và thu hồi phiên; không đưa mật khẩu vào báo cáo.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC12 Đổi mật khẩu

**Tác nhân:** Người dùng đã đăng nhập

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Phiên còn hợp lệ; biết mật khẩu hiện tại.

**Kích hoạt:** Người dùng mở Mật khẩu hoặc bị yêu cầu đổi ban đầu.

**Điều kiện sau:** Mật khẩu mới được hash; các phiên cũ không còn hợp lệ.

### Luồng chính

1. Người dùng nhập mật khẩu hiện tại và mật khẩu mới.
2. Client gửi mutation có CSRF khi dùng cookie.
3. Backend xác minh mật khẩu cũ và giới hạn độ dài mật khẩu mới.
4. Backend cập nhật hash, đổi security stamp và tắt yêu cầu đổi ban đầu.
5. Hệ thống cấp phiên hiện tại mới và thông báo kết quả.

### Luồng thay thế và ngoại lệ

- E1 Mật khẩu cũ sai hoặc mật khẩu mới không hợp lệ: không cập nhật.
- E2 Phiên hết hạn hoặc CSRF lỗi: yêu cầu tải lại/đăng nhập.

**Quy tắc và giới hạn:** Mật khẩu từ 7 đến 128 ký tự; không trả hash ra API hoặc log mật khẩu.

**API và thành phần:** POST /api/auth/change-password

**Tiêu chí nghiệm thu:** Mật khẩu cũ không đăng nhập được sau đổi; token cũ bị thu hồi; không thử thay mật khẩu tài khoản thật khi demo.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC13 Đăng ký chủ quán và gửi lại hồ sơ bị từ chối

**Tác nhân:** Người đăng ký hoặc chủ quán chưa xác minh

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Username chưa dùng; với gửi lại phải có hồ sơ bị từ chối và không còn hồ sơ pending.

**Kích hoạt:** Người dùng đăng ký hoặc chọn yêu cầu xem xét lại.

**Điều kiện sau:** Tạo yêu cầu pending; chưa cấp quyền quản lý địa điểm.

### Luồng chính

1. Người dùng nhập username, tên hiển thị và mật khẩu để gửi đăng ký.
2. Backend tạo user poi_owner chưa verified và review registration pending.
3. Người dùng đăng nhập để xem trạng thái và ghi chú của Admin.
4. Nếu bị từ chối, người dùng nhập ghi chú và gửi yêu cầu xem xét lại.
5. Backend giữ lịch sử, tạo review pending mới và chặn gửi trùng.

### Luồng thay thế và ngoại lệ

- E1 Username trùng hoặc dữ liệu sai: thông báo lỗi; không tạo một phần.
- E2 Đã verified, đang pending hoặc không có lần từ chối: không cho gửi lại.

**Quy tắc và giới hạn:** Chỉ được quản lý POI sau khi Admin duyệt; giấy tờ là tùy chọn cho demo; không dùng CCCD thật.

**API và thành phần:** POST /api/auth/register-owner; GET /api/owner/registration-status; POST /api/owner/registration-resubmission

**Tiêu chí nghiệm thu:** Trước duyệt /owner/places trả 403; gửi lại giữ lý do cũ và không tạo nhiều pending.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC14 Chủ quán gửi đề xuất tạo hoặc sửa địa điểm

**Tác nhân:** Chủ quán đã được duyệt

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Có owner:submit_poi; sửa chỉ áp dụng địa điểm thuộc mình, phiên bản còn mới.

**Kích hoạt:** Chủ quán chọn đề xuất mới hoặc đề xuất sửa.

**Điều kiện sau:** Review submission pending được tạo; nội dung công khai chưa tự đổi.

### Luồng chính

1. Chủ quán mở danh sách địa điểm thuộc mình và chọn thao tác.
2. Chủ quán nhập nội dung, địa chỉ, nguồn, tọa độ và ảnh có chú thích.
3. Client gửi biểu mẫu; backend kiểm tra quyền sở hữu và dữ liệu.
4. Backend kiểm phiên bản và yêu cầu pending đã tồn tại.
5. Hệ thống lưu đề xuất, ghi audit và hiển thị trạng thái chờ duyệt.

### Luồng thay thế và ngoại lệ

- E1 Không thuộc sở hữu hoặc chưa verified: từ chối 403.
- E2 Phiên bản cũ hoặc có đề xuất pending trùng: trả 409 và yêu cầu tải lại.

**Quy tắc và giới hạn:** Owner không trực tiếp công khai POI; bản ghi chính chỉ đổi khi UC15 được phê duyệt.

**API và thành phần:** GET /api/owner/places; GET /api/owner/submissions; POST /api/owner/submissions

**Tiêu chí nghiệm thu:** Owner khác không sửa được; đề xuất không xuất hiện trong catalog trước duyệt.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC15 Admin duyệt đăng ký và đề xuất

**Tác nhân:** Admin có quyền kiểm duyệt và phê duyệt

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Review còn pending; tài khoản hoặc POI liên quan còn hợp lệ.

**Kích hoạt:** Admin mở Kiểm duyệt và chọn quyết định.

**Điều kiện sau:** Review có quyết định, audit và thông báo tới đúng chủ quán.

### Luồng chính

1. Admin mở danh sách yêu cầu và đọc nội dung, nguồn cùng ghi chú.
2. Admin kiểm tra thông tin xác minh tùy chọn nếu có quyền.
3. Admin nhập ghi chú và chọn duyệt hoặc từ chối.
4. Backend kiểm trạng thái, phiên bản và quyền; đăng ký được duyệt làm user verified.
5. Đề xuất được duyệt tạo/cập nhật POI; điều kiện công khai English vẫn áp dụng với dữ liệu chính thức.
6. Backend lưu quyết định và tạo notification liên kết review/địa điểm đúng người nhận.

### Luồng thay thế và ngoại lệ

- E1 Từ chối thiếu lý do: không chấp nhận.
- E2 Đã xử lý, nguồn thay đổi hoặc POI bị xóa: trả 409/404, không ghi đè.

**Quy tắc và giới hạn:** Đọc danh sách cần content:moderate; quyết định cần poi:approve. Dữ liệu demo có ngoại lệ publication gate được ghi rõ.

**API và thành phần:** GET /api/admin/reviews; POST /api/admin/reviews/{id}/decision

**Tiêu chí nghiệm thu:** Không duyệt hai lần; chủ quán nhận đúng ghi chú; đề xuất sửa nguồn cũ không được ghi đè bản mới.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC16 Xem thông báo và đánh dấu đã đọc

**Tác nhân:** Chủ quán đã đăng nhập kể cả chưa verified

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Thông báo thuộc chính tài khoản; có thể chưa có thông báo.

**Kích hoạt:** Chủ quán nhấn chuông hoặc mở URL chi tiết.

**Điều kiện sau:** Số chưa đọc và trạng thái được cập nhật sau khi lưu thành công.

### Luồng chính

1. Hệ thống lấy số chưa đọc cho chuông; chủ quán mở danh sách.
2. Chủ quán có thể lọc chỉ chưa đọc và mở chi tiết bằng URL riêng.
3. Backend kiểm người nhận và quyền trên review/POI liên kết.
4. Chủ quán đọc kết quả duyệt, lý do và ghi chú đã gửi.
5. Chủ quán chọn đánh dấu đã đọc; hệ thống lưu rồi tải lại số đếm.
6. Sau duyệt đăng ký, chủ quán cập nhật quyền truy cập; sau từ chối có thể chuyển tới form gửi lại.

### Luồng thay thế và ngoại lệ

- E1 ID của người khác hoặc không tồn tại trả 404.
- E2 Lỗi tải/đánh dấu: có thông báo và thử lại; không giả số đếm bằng 0.

**Quy tắc và giới hạn:** GET không tự đánh dấu đã đọc; danh sách tối đa 100 gần nhất, lọc chưa đọc giúp tìm bản cũ; có no-store.

**API và thành phần:** GET /api/owner/notifications; GET .../unread-count; GET .../{id}; POST .../{id}/read

**Tiêu chí nghiệm thu:** Kiểm IDOR, read idempotent, URL trực tiếp và tài khoản chưa verified vẫn đọc kết quả của mình.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC17 Admin thêm sửa xóa và công khai địa điểm

**Tác nhân:** Admin có quyền POI tương ứng

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Có quyền create/update/delete; toggle yêu cầu quyền riêng; phiên bản sửa còn mới.

**Kích hoạt:** Admin thao tác trong Địa điểm.

**Điều kiện sau:** Dữ liệu hợp lệ được lưu; catalog và version phản ánh thay đổi.

### Luồng chính

1. Admin chọn thêm hoặc sửa, nhập nội dung, phân loại, nguồn và dữ liệu vị trí.
2. Admin chọn demo, trạng thái công khai và chủ quán hợp lệ nếu cần.
3. Backend xác minh dữ liệu, quyền và UpdatedAt khi sửa.
4. Hệ thống lưu và cập nhật version; dữ liệu chính thức thiếu English/audio được giữ chờ chuẩn bị.
5. Khi xóa, Admin xác nhận; backend xóa POI/menu, tạo tombstone và xử lý đề xuất pending liên quan.

### Luồng thay thế và ngoại lệ

- E1 Validation sai trả 400; sửa đồng thời trả 409.
- E2 Thiếu quyền toggle hoặc owner không đủ điều kiện: không thực hiện. Cascade audio/draft/media chưa đầy đủ.

**Quy tắc và giới hạn:** ID không tái sử dụng; nội dung tối đa 3000 ký tự; bán kính 10–1000 m, priority 0–100. Không ghi đè readiness do client tự gửi.

**API và thành phần:** GET/POST /api/admin/places; PUT/DELETE /api/admin/places/{id}

**Tiêu chí nghiệm thu:** CRUD và restart giữ dữ liệu; bản chưa công khai không hiện cho khách; sửa nguồn làm mất readiness cũ.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC18 Quản lý ảnh bìa và ảnh bổ sung

**Tác nhân:** Admin hoặc chủ quán gửi đề xuất ảnh

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Có quyền sửa hoặc gửi đề xuất; ảnh có nguồn và thông tin sử dụng.

**Kích hoạt:** Người dùng mở phần ảnh trong biểu mẫu POI.

**Điều kiện sau:** Ảnh và crop được lưu khi lưu địa điểm hoặc đề xuất.

### Luồng chính

1. Người dùng nhập URL HTTPS; Admin có thể upload PNG/JPEG/WebP.
2. Backend kiểm dung lượng và signature của file upload.
3. Người dùng nhập chú thích, nguồn ảnh, credit và chỉnh trọng tâm dọc.
4. Người dùng thêm/bỏ ảnh bổ sung và xem preview.
5. Người dùng lưu POI hoặc gửi đề xuất; trang khách hiển thị bộ ảnh theo dữ liệu API.

### Luồng thay thế và ngoại lệ

- E1 Ảnh quá 5 MB hoặc sai loại bị từ chối.
- E2 Nguồn thiếu/URL sai hoặc quá số lượng: validation lỗi. Upload thành công chưa đồng nghĩa POI đã lưu.

**Quy tắc và giới hạn:** Hiện tối đa 6 ảnh bổ sung và 1 bìa, tổng 7; HTML yêu cầu 8 nên còn lệch. Owner hiện nhập URL, không có nút upload riêng.

**API và thành phần:** POST /api/media; GET /api/media/{name}; API lưu POI hoặc submission

**Tiêu chí nghiệm thu:** Gallery CRUD và stale update có tests; kiểm ghi chú/crop/ảnh lỗi; không dùng ảnh sai địa điểm không chú thích.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC19 Quản lý thực đơn

**Tác nhân:** Admin hoặc chủ quán đã được duyệt

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Có quyền menu phù hợp; owner chỉ quản lý địa điểm thuộc mình.

**Kích hoạt:** Người dùng mở Thực đơn trong trình sửa POI.

**Điều kiện sau:** Danh sách món thay đổi và có thể được khách xem qua API.

### Luồng chính

1. Hệ thống tải thực đơn của địa điểm.
2. Người dùng nhập tên, mô tả và giá tham khảo hoặc để giá chưa xác minh.
3. Người dùng thêm mới hoặc chọn món hiện có để sửa.
4. Backend kiểm quyền, ownership, dữ liệu và lưu món.
5. Admin có quyền xóa có thể xác nhận xóa; giao diện tải lại danh sách.

### Luồng thay thế và ngoại lệ

- E1 Giá âm, tên trống hoặc món không tồn tại: từ chối.
- E2 Owner truy cập POI khác bị chặn; owner mặc định không có xóa món.

**Quy tắc và giới hạn:** Giá là tham khảo VND; null được hiển thị là chưa xác minh, không thay bằng giá bịa.

**API và thành phần:** GET/POST /api/{admin|owner}/places/{id}/menu; PUT .../menu/{itemId}; DELETE lane admin

**Tiêu chí nghiệm thu:** HTTP fixture thêm/sửa/đọc/xóa đã chạy; dữ liệu demo hiện không cần bịa thực đơn.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC20 Quản lý tài khoản

**Tác nhân:** Admin có quyền user tương ứng

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Vai trò được cấp không vượt quyền người thao tác.

**Kích hoạt:** Admin mở Tài khoản và tạo hoặc cập nhật người dùng.

**Điều kiện sau:** Tài khoản có trạng thái và quyền mới; thay đổi nhạy cảm thu hồi phiên cũ.

### Luồng chính

1. Admin tải danh sách tài khoản và các vai trò được phép cấp.
2. Admin tạo user với username, tên hiển thị, mật khẩu ban đầu và vai trò.
3. Admin có thể đổi role, khóa hoặc mở khóa tài khoản.
4. Backend kiểm quyền cấp role và bảo vệ tài khoản quản trị cuối cùng.
5. Admin xóa tài khoản đủ điều kiện; backend xử lý dữ liệu tài khoản liên quan và ghi audit.

### Luồng thay thế và ngoại lệ

- E1 Không tự khóa/xóa mình hoặc xóa super_admin; không làm mất super_admin hoạt động cuối cùng.
- E2 Tài khoản đang sở hữu POI phải chuyển chủ trước khi xóa; username trùng trả 409.

**Quy tắc và giới hạn:** API không trả password hash; đổi role/enabled đổi security stamp; chỉ hiển thị role có thể cấp.

**API và thành phần:** GET/POST /api/admin/users; PUT/DELETE /api/admin/users/{id}; GET /api/admin/assignable-roles

**Tiêu chí nghiệm thu:** HTTP fixture tạo/đổi/khóa/mở/xóa đạt; kiểm không cấp quyền cao hơn và không đọc hash.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC21 Quản lý vai trò và quyền

**Tác nhân:** Super Admin hoặc tài khoản có role permissions

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Có quyền quản lý role; không cấp quyền vượt quyền của mình.

**Kích hoạt:** Người dùng mở Vai trò.

**Điều kiện sau:** Vai trò tùy chỉnh và tập quyền được cập nhật hợp lệ.

### Luồng chính

1. Hệ thống tải danh sách role và danh mục 32 quyền.
2. Người quản trị nhập tên role và chọn các quyền cần thiết.
3. Backend kiểm tên, quyền hợp lệ và tập quyền của người cấp.
4. Người quản trị có thể sửa role tùy chỉnh, lưu và kiểm lại danh sách.
5. Khi xóa, backend kiểm role không phải built-in và chưa được tài khoản sử dụng.

### Luồng thay thế và ngoại lệ

- E1 Role built-in không được sửa hoặc xóa bằng thao tác tùy chỉnh.
- E2 Role đang dùng hoặc trùng tên bị từ chối; quyền vượt mức trả 403.

**Quy tắc và giới hạn:** Bốn vai trò mặc định: super_admin, admin, poi_owner, user. Có role trong UI không đồng nghĩa được thực hiện mọi API.

**API và thành phần:** GET/POST /api/admin/roles; PUT/DELETE /api/admin/roles/{name}

**Tiêu chí nghiệm thu:** Tạo/sửa/xóa role fixture thành công; built-in và last-super-admin có test bảo vệ.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC22 Biên tập duyệt và thu hồi bản dịch

**Tác nhân:** Admin biên tập và người có quyền công bố

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Có poi:update; duyệt/thu hồi cần thêm content:moderate và content:publish.

**Kích hoạt:** Admin mở Bản dịch thuyết minh.

**Điều kiện sau:** Bản công khai chỉ đổi sau khi MP3 của đúng bản nháp sẵn sàng.

### Luồng chính

1. Admin chọn POI và một trong năm ngôn ngữ ngoại ngữ; xem nguồn Việt.
2. Admin lấy bản dịch hiện tại hoặc tự biên tập và lưu nháp cùng sourceHash/version.
3. Người có đủ quyền yêu cầu duyệt; backend kiểm nguồn và bản nháp chưa thay đổi.
4. Backend tạo MP3 Edge-TTS trước, kiểm lại quyền rồi publish revision.
5. Catalog và audio dùng revision mới; Admin nghe bản công khai.
6. Khi thu hồi, backend kiểm revision và điều kiện English của POI chính thức.

### Luồng thay thế và ngoại lệ

- E1 Nguồn hoặc draft cũ trả 409; chưa lưu không được duyệt.
- E2 TTS lỗi giữ bản công khai trước; không thu hồi English duy nhất đang bảo đảm POI chính thức công khai.

**Quy tắc và giới hạn:** Sửa Việt trong POI; bản dịch không được giữ nguyên toàn bộ Việt. Bản duyệt gắn sourceHash/lang; MP3 và gói offline cũ không bị xóa ngay.

**API và thành phần:** GET/PUT /api/admin/translations/{placeId}/{language}; POST .../approve; POST .../withdraw

**Tiêu chí nghiệm thu:** Draft riêng tư, lỗi TTS giữ bản cũ, stale revision và quyền publish có integration tests.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC23 Tạo và điều khiển tác vụ audio

**Tác nhân:** Admin có poi:update

**Trạng thái:** Đã triển khai

**Điều kiện trước:** POI còn tồn tại; không có task queued/running/paused trùng địa điểm.

**Kích hoạt:** Admin chọn POI, ngôn ngữ và tạo tác vụ.

**Điều kiện sau:** Task và tiến độ được lưu; trạng thái hiển thị qua SSE.

### Luồng chính

1. Admin chọn địa điểm và từ một đến sáu ngôn ngữ.
2. Backend tạo task queued, ưu tiên English trước trong danh sách ngôn ngữ.
3. Worker thực hiện tối đa ba task và dùng nguồn đã chụp hash.
4. Giao diện nhận SSE, hiển thị completed/total cùng lỗi nếu có.
5. Admin có thể pause, resume hoặc cancel theo trạng thái.
6. Khi khởi động lại, task running được đưa về queued để xử lý tiếp.

### Luồng thay thế và ngoại lệ

- E1 Task trùng đang chạy trả 409; ngôn ngữ sai bị từ chối.
- E2 Nguồn đổi hoặc TTS lỗi: task failed; task stale khi process còn chạy chưa có watchdog hoàn chỉnh.

**Quy tắc và giới hạn:** Retention task 14 ngày, heartbeat 5 giây; pause không phải hoàn tất MP3 hiện hành. Recovery bằng cache đã test, chưa kill Python giữa tác vụ thật.

**API và thành phần:** GET/POST /api/admin/audio-tasks; POST .../{id}/{pause|resume|cancel}; GET .../stream

**Tiêu chí nghiệm thu:** Kiểm transition hợp lệ, persist/restart, sourceHash và task thất bại; không gọi fixture MP3 là audio thật.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC24 Xem tổng quan và xuất thống kê

**Tác nhân:** Admin có analytics:view hoặc chủ quán có analytics:view_own

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Đã đăng nhập; xuất CSV cần analytics:export.

**Kích hoạt:** Người dùng mở Tổng quan hoặc chọn xuất CSV.

**Điều kiện sau:** Hiển thị chỉ số theo quyền; CSV có sự kiện được phép xuất.

### Luồng chính

1. Hệ thống xác định quyền quản trị hoặc ownership.
2. Admin xem số POI, công khai, tài khoản, pending review và event được đồng ý.
3. Hệ thống hiển thị thiết bị ẩn danh hoạt động trong 5 phút và độ dài queue.
4. Chủ quán chỉ xem thống kê gắn với POI thuộc mình.
5. Admin có quyền xuất tải CSV, không tính heartbeat là lượt xem/nghe.

### Luồng thay thế và ngoại lệ

- A1 Chưa có consent event: chỉ số 0 là hợp lệ.
- E1 Không đủ quyền bị chặn; số liệu không đại diện toàn bộ khách truy cập.

**Quy tắc và giới hạn:** Nguồn hiện là JSON một tiến trình, không phải read model Redis/Mongo phân tán; không có GPS trong analytics.

**API và thành phần:** GET /api/admin/analytics; GET /api/admin/analytics/export; GET /api/owner/analytics

**Tiêu chí nghiệm thu:** Không tính heartbeat vào event view/play; owner không xem POI người khác; export không trả mật khẩu hoặc giấy tờ.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC25 Xem nhật ký quản trị

**Tác nhân:** Admin có audit:read

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Có phiên đăng nhập và quyền xem audit.

**Kích hoạt:** Admin mở Nhật ký.

**Điều kiện sau:** Các hành động gần nhất được hiển thị theo thời gian.

### Luồng chính

1. Admin mở màn nhật ký; client gọi API có xác thực.
2. Backend kiểm audit:read và giới hạn số bản ghi.
3. Hệ thống trả thời gian, actor, action và resource.
4. Admin đối chiếu thao tác đã thực hiện với bản ghi phù hợp.

### Luồng thay thế và ngoại lệ

- A1 Chưa có bản ghi: hiển thị bảng rỗng.
- E1 Thiếu quyền hoặc phiên hết hạn: từ chối truy cập, không trả dữ liệu cache công khai.

**Quy tắc và giới hạn:** Limit mặc định 100, tối đa 500 bản ghi mỗi lần đọc; không có chức năng tìm kiếm/xuất audit hoàn chỉnh trong UI hiện tại.

**API và thành phần:** GET /api/admin/audit?limit=100

**Tiêu chí nghiệm thu:** Tạo/sửa/xóa/duyệt ghi đúng actor và tài nguyên; không đưa mật khẩu/CCCD rõ vào audit.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC26 Quản lý thông tin xác minh chủ quán tùy chọn

**Tác nhân:** Chủ quán và Admin có quyền đọc xác minh

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Owner có owner:access; Admin cần user:read cùng poi:approve.

**Kích hoạt:** Owner lưu hoặc xóa thông tin; Admin mở thông tin khi xét đăng ký.

**Điều kiện sau:** Dữ liệu lưu mã hóa, owner chỉ nhận mask; đọc của Admin có audit.

### Luồng chính

1. Owner đọc hướng dẫn tùy chọn và nhập số 12 chữ số dùng cho kiểm thử.
2. Backend kiểm dữ liệu, mã hóa bằng ASP.NET Data Protection và lưu expiry 180 ngày.
3. Owner xem trạng thái cùng phần che số hoặc chọn xóa.
4. Admin đủ quyền xem thông tin khi duyệt; backend kiểm hạn và giải mã nếu hợp lệ.
5. Hệ thống ghi audit đọc; bản hết hạn không được trả và có cleanup định kỳ.

### Luồng thay thế và ngoại lệ

- E1 Dữ liệu sai định dạng bị từ chối; owner không quản lý giấy tờ của người khác.
- E2 Ciphertext bị sửa, hết hạn hoặc không giải mã được: trả không có dữ liệu hợp lệ, không lộ nội dung.

**Quy tắc và giới hạn:** Không bắt buộc cho demo; chỉ dùng số giả lập, không yêu cầu nhóm nhập giấy tờ thật. Key bảo vệ không đưa vào Git.

**API và thành phần:** GET/PUT/DELETE /api/owner/identity; GET /api/admin/users/{id}/identity

**Tiêu chí nghiệm thu:** Tests masking, encryption, tamper, expiry và IDOR đạt; không dùng dữ liệu cá nhân thật làm minh chứng.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC27 Theo dõi vị trí đã được khách đồng ý

**Tác nhân:** Admin có system:logs

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Có quyền giám sát; chỉ có dữ liệu khi khách đã dùng UC10.

**Kích hoạt:** Admin mở Giám sát vị trí và chọn cửa sổ thời gian.

**Điều kiện sau:** Hiển thị bản ghi ẩn danh thuộc cửa sổ, không suy ra mọi khách truy cập.

### Luồng chính

1. Admin mở màn giám sát; hệ thống kiểm quyền.
2. Admin chọn khoảng 1, 6 hoặc 24 giờ.
3. Backend lọc read model vị trí phiên/giờ trong thời gian giữ dữ liệu.
4. UI hiển thị mã phiên rút gọn, thời điểm, tọa độ đã làm tròn và accuracy.
5. Admin đổi cửa sổ để tải lại hoặc thấy thông báo chưa có vị trí được đồng ý.

### Luồng thay thế và ngoại lệ

- A1 Không có bản ghi là trạng thái hợp lệ, không phải bản đồ GPS mô phỏng.
- E1 Không có system:logs hoặc hours ngoài 1–24: từ chối.

**Quy tắc và giới hạn:** Lưu tối đa 24 giờ; vị trí không gắn tài khoản và không trộn analytics. Backend trả tối đa 500 bản ghi cửa sổ.

**API và thành phần:** GET /api/runtime/window?hours=1

**Tiêu chí nghiệm thu:** Kiểm quyền, cửa sổ, rounding và forget bằng dữ liệu giả lập; chưa xác minh tuyến thực địa.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026

## UC28 Kiểm tra khả năng chạy hệ thống

**Tác nhân:** Admin có system:logs; health công khai cho client

**Trạng thái:** Đã triển khai

**Điều kiện trước:** Kho dữ liệu và cấu hình có thể được kiểm tra.

**Kích hoạt:** Admin mở Kiểm tra hệ thống hoặc client probe health.

**Điều kiện sau:** Hiển thị trạng thái có ý nghĩa đúng phạm vi kiểm tra.

### Luồng chính

1. Public client gọi health để biết kho cốt lõi có sẵn sàng.
2. Admin mở chẩn đoán; backend kiểm quyền và không thay đổi catalog.
3. Hệ thống kiểm JSON và quyền ghi thư mục bằng probe riêng.
4. Hệ thống kiểm manifest map, MP3 đúng nguồn và cấu hình worker/AI/weather.
5. UI hiển thị kết quả, số MP3 theo ngôn ngữ và giới hạn kiểm tra.

### Luồng thay thế và ngoại lệ

- E1 Kho hỏng hoặc không ghi được: public readiness trả 503.
- A1 Thiếu cấu hình AI/weather không đồng nghĩa phần lõi ngừng hoạt động.

**Quy tắc và giới hạn:** Readiness kho cache tối đa 15 giây; cờ cấu hình không chứng minh đã kết nối provider hoặc triển khai production.

**API và thành phần:** GET /health/ready; GET /api/health/ready; GET /api/admin/system-status

**Tiêu chí nghiệm thu:** Health không lộ đường dẫn/secret; tests kiểm lỗi JSON, cache thời gian và owner không đọc được chẩn đoán.

**Bằng chứng:** Kiểm tra mã nguồn và kiểm thử tự động ngày 20/10/2026
