# Tái sử dụng, vận hành và mở rộng SmartTour

## 1. Phạm vi hiện tại

Bản hiện tại phù hợp demo và triển khai một máy chủ với nhiều trình duyệt truy cập. SQLite là SQL database nhúng, không phải SQL Server. Mỗi nhóm bản ghi có bảng riêng, nội dung phức hợp nằm trong cột JSON `payload`; analytics có cột thiết bị/thời gian và index để truy vấn trực tiếp. Chưa phải mô hình quan hệ chuẩn hóa toàn bộ nghiệp vụ.

Không suy ra khả năng phục vụ hàng nghìn người từ thử nghiệm vài chục request. Các tác vụ còn đọc snapshot nghiệp vụ trong bộ nhớ; hàng đợi thống kê và lịch sử được giới hạn. Khi cần nhiều API replica hoặc tải lớn, thay implementation `IPlatformStore`, chuyển sang PostgreSQL/SQL Server, repository theo từng nghiệp vụ và worker có cơ chế nhận tác vụ độc quyền. Worker hiện tại dành cho một API instance.

## 2. Trách nhiệm các lớp

| Phần | Trách nhiệm | Điểm mở rộng |
|---|---|---|
| React / `api.js` | Giao diện, gọi API cùng origin, thông báo lỗi | Không rải URL localhost vào component |
| `NearbyPlaces`, `useNearbyLocation`, `locationTracking` | Hiển thị gợi ý, vòng đời GPS, loại dữ liệu cũ | Có thể thay nguồn vị trí qua adapter để test |
| API controllers | HTTP, đăng nhập, CSRF, quyền, giới hạn request | Controller gọi Business, không viết SQL thống kê |
| Business | Quy tắc địa điểm, tài khoản, bản dịch và thống kê | `IPlatformStore`, `IPlaceRepository`, `IDeviceActivityReader` |
| Data / `SqlitePlatformStore` | Transaction, bảng/index, nhập dữ liệu JSON cũ | Thay storage mà giữ hợp đồng Business |
| Docker / Nginx / Cloudflare | Đóng gói và chuyển request tới API | Local, tunnel tạm hoặc tunnel tên miền cố định |

Các lần ghi SQL dùng transaction immediate để tránh mất cập nhật giữa các kết nối. Chỉ thay dòng đã đổi, giữ dòng lịch sử không đổi. Endpoint danh sách địa điểm chỉ đọc bảng địa điểm và bản dịch, không tải toàn bộ thống kê/tài khoản. Bảng thiết bị có phân trang, polling 15 giây; polling hủy khi rời trang. Các phiên bản đầu vẫn tương thích dữ liệu JSON để đối chiếu và test.

## 3. Đếm và phân biệt thiết bị

Khách bật đồng ý thống kê trong Tiện ích. Trình duyệt tạo UUID lưu tại localStorage; gửi heartbeat khoảng 60 giây khi tab đang hiển thị và có mạng. Worker xử lý batch khoảng 2 giây. Admin thấy thiết bị hoạt động trong 5 phút gần nhất, loại Mobile/Tablet/Desktop, họ trình duyệt, hệ điều hành, lần đầu/cuối trong dữ liệu còn giữ, lượt xem và nghe.

Chỉ gửi các nhãn phân loại cho phép, không gửi toàn bộ User-Agent, tên máy, MAC hoặc vị trí GPS qua luồng thống kê. Các nhãn trình duyệt do client cung cấp chỉ dùng tham khảo. Nhiều tab dùng chung mã trong cùng origin/trình duyệt. Trình duyệt khác, ẩn danh, xóa localStorage hoặc đổi hostname có thể thành mã mới. Đóng tab không có nghĩa số online giảm ngay: chờ cửa sổ 5 phút. Tab nền không gửi heartbeat thường xuyên.

`POST /api/analytics/batch` nhận sự kiện có đồng ý. `POST /api/analytics/forget` xóa sự kiện, hàng đợi, heartbeat và mô tả của mã đó. `GET /api/admin/devices?page=1&pageSize=25` yêu cầu quyền `analytics:view`. Không trả bảng thiết bị cho khách chưa đăng nhập. Giữ tối đa 30 ngày/10.000 sự kiện, nên đây không phải tổng truy cập trọn đời.

## 4. Gợi ý địa điểm gần

Ngay khi mở trang trên HTTPS/localhost, hệ thống tự bắt đầu `watchPosition`; không cần nút bật gợi ý. Lần đầu khách vẫn cần cho phép quyền vị trí của trình duyệt; website không thể vượt qua quyền này. Danh sách cập nhật khi nhận GPS mới, hạn chế cập nhật tối đa một lần mỗi 5 giây. Mẫu quá 30 giây hoặc ở tương lai bị loại. Khi mất GPS mới, gợi ý cũ được bỏ và hiển thị chờ vị trí. Dừng định vị/rời trang sẽ `clearWatch`, hủy timer và bỏ callback đến muộn. Nếu bị từ chối, không hỏi lặp liên tục; khách có thể cấp lại quyền rồi thử lại.

Danh sách lọc bán kính 1/3/5/10 km, chỉ lấy địa điểm có tọa độ xác minh, sắp xếp theo Haversine. Đây là khoảng cách đường thẳng, không phải dẫn đường đi bộ hay thời gian di chuyển. Nếu ngoài khu vực thì không tạo địa điểm giả. Sai số GPS được hiển thị. Chọn một gợi ý mở phần giới thiệu/thuyết minh.

Tiện ích **Bật định vị & thuyết minh gần bạn** là chế độ geofence đã có: debounce khi vào vùng, tránh kích hoạt lặp và cooldown. Quyền GPS không đồng nghĩa đồng ý thống kê/chia sẻ vị trí với Admin. Trình duyệt có thể yêu cầu tương tác trước khi phát âm thanh; dùng nút nghe nếu tự phát bị chặn. Cần thử đi bộ thực tế trên điện thoại để kiểm chứng ngoài môi trường test.

## 5. Link công khai và tái sử dụng

```powershell
& ./scripts/Start-PublicDemo.ps1 -Build   # cập nhật source và chạy
& ./scripts/Start-PublicDemo.ps1          # chạy lại image có sẵn
& ./scripts/Start-PublicDemo.ps1 -Action Status
& ./scripts/Start-PublicDemo.ps1 -Action Logs
& ./scripts/Start-PublicDemo.ps1 -Action Stop
```

Quick Tunnel dùng link tạm, có thể đổi khi tạo lại, giới hạn 200 request đang xử lý đồng thời và không hỗ trợ SSE. Bảng tác vụ audio dùng polling để tương thích. Đây là giới hạn request, không phải cam kết hỗ trợ 200 người. Nguồn: https://developers.cloudflare.com/tunnel/get-started/quick-tunnels/

Để có tên miền cố định, tạo Cloudflare Tunnel trong tài khoản của bạn, cấu hình hostname tới service `http://web:80`, thêm `SMARTTOUR_CLOUDFLARE_TOKEN` vào `.env.docker` bị Git ignore, sau đó chạy:

```powershell
& ./scripts/Start-PublicDemo.ps1 -Named -Build
```

Chỉ dùng một trong hai override quick/named. Bản named đã chuẩn bị cấu hình; chưa thể xác minh hostname cố định nếu chưa có tài khoản/tên miền/token của chủ dự án. Muốn truy cập 24/7 khi tắt laptop cần chuyển các container và dữ liệu sang máy chủ luôn bật. Cloudflare Tunnel chuyển tiếp về máy chủ, không thay thế máy chủ.

Nginx và API dùng cùng origin nên không cần hardcode domain Cloudflare trong React. API không publish cổng trực tiếp; origin HTTP bind loopback. Nếu thay mô hình reverse proxy/network, rà lại cấu hình trusted forwarded headers trong `Program.cs` và `docker/nginx.conf`.

## 6. SQL và backup

Docker dùng `/app/App_Data/smarttour.db` trong volume `smarttour-demo_smarttour-data`. Khóa mã hóa nằm trong volume `smarttour-demo_smarttour-keys`. Chạy ngoài Docker mặc định dùng SQLite trong `ShortVideoTranslator.Api/App_Data`. Đổi `Storage__Provider=Json` chỉ khi chủ động dùng kho JSON riêng; hai kho không đồng bộ sau lần nhập đầu.

Truy vấn bằng SQLite (DB Browser for SQLite có thể mở bản sao):

```sql
SELECT COUNT(DISTINCT deviceId) AS active_devices
FROM analyticsHeartbeats
WHERE julianday(lastSeen) > julianday('now', '-5 minutes');

SELECT deviceId, kind, createdAt FROM events ORDER BY createdAt DESC LIMIT 50;

SELECT deviceId,
       json_extract(payload,'$.descriptor.browser') AS browser,
       json_extract(payload,'$.descriptor.operatingSystem') AS os
FROM analyticsDevices;
```

Sao lưu đầy đủ trước khi chuyển máy:

```powershell
& ./scripts/Backup-Demo.ps1
```

Script tạm dừng API, sao chép App_Data, khóa DataProtection và cấu hình `.env.docker` sang `artifacts/backups/<timestamp>`, rồi khởi động API lại. Website tạm gián đoạn trong lúc sao lưu. Thư mục backup chứa tài khoản/cấu hình riêng, không đưa lên Git. Không chỉ copy file `.db` khi đang ghi WAL.

Máy mới: lấy source, cài Docker, chuẩn bị `.env.docker` từ backup và build container trước. Dừng API, copy **nội dung** App_Data và DataProtection-Keys từ backup vào đúng hai volume bằng `docker cp` tới container API đã tạo. Nếu file được copy bằng tài khoản root, chạy container một lần với `--user root --entrypoint chown` để đặt lại chủ sở hữu `app:app` cho `/app/App_Data` và `/home/app/.aspnet/DataProtection-Keys`, rồi bật API. Đối chiếu `/api/health`, đăng nhập, số POI, thuyết minh và thống kê. Không dùng `down -v` vì xóa volume dữ liệu. Quy trình khôi phục cần thực hiện trên bản sao trước khi thay kho đang dùng.

## 7. Checklist trước khi bàn giao

- `dotnet build`, `dotnet test`, Frontend lint/test/build đạt.
- Docker healthchecks healthy; `/api/health` báo `sqlite`.
- Mở link HTTPS trên hai trình duyệt/điện thoại, đồng ý thống kê; Admin thấy các mã khác nhau và mô tả đúng.
- Tắt một trình duyệt, đợi quá 5 phút; không còn active.
- Bật GPS, di chuyển và kiểm tra gợi ý đổi thứ tự; thử từ chối quyền và tắt GPS.
- Thử nghe MP3 đúng địa điểm/ngôn ngữ; kiểm tra offline sau khi tải gói.
- Thử backup và khôi phục trên kho riêng; không coi chỉ có script là đã thử khôi phục thực tế.
