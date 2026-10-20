# AI Advisor và readiness — 15/10/2026

Đối chiếu chapter Admin/AI và startup readiness của HTML; giữ ASP.NET Core/C#/React. Máy demo hiện **chưa cấu hình AI hoặc OpenWeather**. Các ca provider thành công, fallback và weather dùng HTTP fixtures trong xUnit; không phải bằng chứng đã gọi mô hình hay thời tiết thật.

## Dùng trên giao diện

Admin hoặc chủ quán được duyệt → Địa điểm → Sửa → **Trợ lý AI biên tập**. Bảng hiển thị trạng thái cấu hình, hạn mức, giờ đặt lại. Khi chưa có provider, nút tạo bị khóa và vẫn sửa thuyết minh thủ công được.

Khi đã cấu hình: tạo bản nháp từ tên/danh mục/địa chỉ/nội dung đang nhập. Xem nguồn và đề xuất cạnh nhau; tích xác nhận đã rà soát rồi Đưa vào ô thuyết minh, hoặc Bỏ đề xuất. Việc đưa vào ô **chưa lưu/công khai**; tiếp tục qua nút Lưu địa điểm/luồng owner duyệt đang có. Đổi nguồn sẽ hủy yêu cầu đang chờ và ẩn bản nháp không còn khớp. Provider trả về được ghi rõ; lỗi provider trước khi fallback có thông báo riêng.

Nguồn ngắn không bị kéo đủ200–300 từ bằng dữ kiện tự bịa. Prompt hướng đến200–300 từ khi đủ nguồn, tối đa3000ký tự. Bản nháp AI không thay thế kiểm chứng lịch sử/địa chỉ.

## Cấu hình backend

Dùng cấu hình .NET hoặc biến môi trường trước khi chạy API; không thêm khóa thật vào repository hay biến VITE.

| Biến | Giá trị/ý nghĩa |
|---|---|
| `Ai__Provider` | `Auto` mặc định, `Gemini` hoặc `ProxyPal` |
| `Ai__ApiKey` | Khóa Gemini; không dùng khóa test |
| `Ai__Model` | Model Gemini hợp lệ trên tài khoản của bạn; không gán model mặc định có thể đã ngừng phục vụ |
| `Ai__ProxyPal__BaseUrl` | URL API-compatible do bạn vận hành, ví dụ `http://localhost:8317/v1` |
| `Ai__ProxyPal__ApiKey` | Khóa gateway ProxyPal |
| `Ai__ProxyPal__Model` | Tên model gateway thực sự cung cấp |
| `Weather__ApiKey` | Khóa OpenWeather, chỉ cần nếu dùng thời tiết |

`Auto` thử Gemini nếu được cấu hình, sau đó ProxyPal nếu Gemini lỗi và ProxyPal đã được cấu hình. Mỗi attempt tối đa12giây khi có hai provider; một provider tối đa25giây; toàn pipeline tối đa30giây. Proxy dùng `POST {BaseUrl}/chat/completions`, bearer, model/messages/stream=false; Gemini giữ API generateContent cũ. Chỉ chấp nhận HTTPS remote hoặc HTTP loopback; URL không có userinfo/query/fragment; không theo redirect khi gửi key. Chưa cấu hình/response rỗng/hỏng/quota upstream/hết thời gian trả503, không giả lập nội dung thành công.

ProxyPal không tự tạo quyền truy cập mô hình: cần gateway đang chạy và tài khoản/provider hợp lệ. Cờ `ready`/`configurationOnly` chỉ cho biết cấu hình đủ, không chứng minh kết nối. Nguồn giao thức: [ProxyPal chính thức](https://github.com/heyhuynhgiabuu/proxypal).

## Hạn mức và hủy

Owner10bản nháp **tạo thành công** mỗi ngày UTC; UI đổi giờ đặt lại sang giờ thiết bị. Admin không giới hạn. Reserve trong bộ nhớ trước khi gọi để10yêu cầu đồng thời không vượt hạn mức; chỉ ghi số lượt sau khi nhận bản nháp hợp lệ và kiểm tra lại quyền. Failure/cancel trước hoàn tất trả reservation. Restart thả reservation chưa hoàn tất; lượt thành công lưu trong JSON, giữ7ngày. Nếu browser hủy sau khi server đã hoàn tất, lượt thành công vẫn có thể được tính.

GET `/api/ai/usage`: giữ `used`/`limit` cấp trên để tương thích, thêm providers/configurationOnly/weatherReady/quota(inFlight,remaining,resetsAt). POST `/api/ai/enhance-description` nhận text/name/category/address/placeId/isDemo/useWeather. Cookie cần CSRF; bearer theo quyền tương tự. Owner phải được duyệt và chỉ dùng placeId thuộc mình; admin có poi:update.

## Thời tiết tùy chọn

Mặc định tắt trên mỗi biểu mẫu. Chỉ khi người biên tập chọn dùng thời tiết và POI đã có tọa độ xác minh, server mới gửi tọa độ **địa điểm** tới OpenWeather. Không đọc/gửi GPS của khách. POI mới chưa lưu không dùng weather. Thiếu khóa, lỗi upstream, thời gian quan trắc cũ hơn1giờ/future hơn5phút hoặc payload sai → bỏ weather, vẫn có thể tạo bản nháp AI với cảnh báo rõ ràng; không đoán thời tiết.

Gọi current weather với metric/lang=vi, timeout5giây, cache theo tọa độ tối đa10phút và không vượt tuổi dữ liệu1giờ; tối đa100entry. HTTP client weather không ghi log URL có appid. Preview hiển thị thời điểm quan trắc, không coi là dự báo cho ngày tham quan. [Tài liệu OpenWeather chính thức](https://openweathermap.org/current).

## Kiểm tra hệ thống

Admin → **Kiểm tra hệ thống** cần `system:logs`. Bảng kiểm tra JSON/ghi thư mục bằng probe tạm tự xóa, gói bản đồ checksum, MP3 đúng nội dung từng ngôn ngữ; phân biệt “Đã cấu hình” với “đã gọi dịch vụ thật”. Probe không tăng catalog version và không ghi vào platform.json. Cache kết quả kho tối đa15giây. `/health`, `/health/ready`, `/api/health/ready` public chỉ trả trạng thái/thời điểm; trả503 khi kho chưa sẵn sàng, không lộ đường dẫn hoặc khóa. Optional AI/weather thiếu khóa không chặn core readiness. Giữ endpoint `/api/health` cũ tương thích.

Readiness không thay thế kiểm thử tạo MP3 mới, đầy dung lượng, ACL riêng từng file, nhiều instance hoặc triển khai production. Kho JSON hiện dành cho demo một tiến trình.
