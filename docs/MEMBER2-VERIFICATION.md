# Thành viên 2 — GPS, giao diện, thuyết minh và offline

## Phạm vi và trạng thái bàn giao

Đã triển khai phần frontend và chạy kiểm thử tự động trong cloud trên Chromium.
Chưa đủ điều kiện xác nhận hoàn thành kiểm thử điện thoại thật: chưa có kết quả
Android/iPhone, giọng đọc dịch vụ thật hoặc bản đồ PMTiles thật trong lượt này.
Không dùng ảnh mô phỏng để chứng minh GPS hay giọng đọc trên thiết bị thật.

## Thay đổi

- Trang tự yêu cầu quyền GPS khi mở; vẫn phải được người dùng/trình duyệt cho phép.
  Không tự bật GPS hệ điều hành và không tự gửi tọa độ về Admin.
- Thêm danh sách gần bạn, bán kính 1/3/5/10 km, khoảng cách và thứ tự cập nhật
  theo vị trí. Chỉ dùng địa điểm đã xác minh và GPS chính xác trong 100 m.
- Thông báo riêng cho từ chối quyền, timeout, không có GPS và trang HTTP không
  bảo mật. Dừng định vị xóa vị trí hiển thị, hủy watcher và bỏ callback đến muộn.
- Bỏ dữ liệu vị trí cũ hơn 30 giây, từ tương lai hoặc tọa độ/độ chính xác không hợp lệ.
  Tín hiệu kém cũng xóa trạng thái chờ vào vùng thuyết minh, tránh kích hoạt sai khi hồi phục.
- Bổ sung điều khiển gần bạn phù hợp màn hình nhỏ và thông tin GPS trong hộp mở đầu.
- Thêm kiểm thử đơn vị và bộ regression trình duyệt có fixture độc lập.

## Kết quả đã chạy

Môi trường: Node 24, Chromium headless, Playwright 1.62.1, Vite production build.
Browser test dùng API và vị trí **mô phỏng**, ảnh PNG fixture, âm WAV tạo bằng code.
Không gọi API production, không đưa địa điểm thử vào dữ liệu thật.

| Kiểm tra | Kết quả | Phạm vi bằng chứng |
|---|---|---|
| Lint và production build | Đạt | `npm run lint`, `npm run build` |
| Unit tests | 52/52 đạt | Bản tích hợp main và thành viên 2; gồm 5 bài mới về watcher, lỗi quyền, dữ liệu GPS, bán kính và debounce |
| Browser regression | 10/10 đạt | [Kết quả JSON](member2-evidence/results.json) |
| Tự yêu cầu GPS, 1/3/5/10 km | Đạt mô phỏng | Không bấm nút bật lúc vào trang; kiểm tra số địa điểm theo từng bán kính |
| Di chuyển, không có địa điểm, tín hiệu kém | Đạt mô phỏng | Thay tọa độ, kiểm tra thứ tự/khoảng cách và trạng thái rỗng |
| Từ chối và dừng/bật lại GPS | Đạt mô phỏng | Vẫn chọn thủ công; watcher bị hủy, không cập nhật khi dừng |
| Responsive | Đạt Chromium | 320, 390, 768, 1280 px; không tràn trang ngang |
| Nghe/dừng/phát lại, đổi địa điểm/ngôn ngữ | Đạt với WAV fixture | Có giải mã/phát audio trong browser, không xác nhận giọng đọc TTS |
| Đổi địa điểm lúc tải | Đạt | Trì hoãn tải 1,8 giây; không phát audio của địa điểm cũ |
| Offline shell/catalog/ảnh/audio đã lưu | Đạt với fixture | Thực sự đặt browser offline rồi reload; không giả lập phản hồi API khi offline |
| Gói đầy đủ thiếu bản đồ | Đạt xử lý lỗi | Thiếu PMTiles thì báo lỗi, không báo hoàn tất sai |
| GPS Android/iPhone thật | **Chưa chạy** | Cần thiết bị thật và HTTPS |
| Gói đầy đủ có bản đồ PMTiles thật | **Chưa chạy** | Cần chuẩn bị gói và MP3 thật cho mọi địa điểm |
| Chất lượng giọng đọc/ngôn ngữ | **Chưa xác minh** | Browser chỉ kiểm tra luồng vi/en bằng âm mẫu; zh/ja/ko/fr chưa thử thao tác |

Ảnh đều là dữ liệu mô phỏng:

- [Danh sách gần bạn trên mobile](member2-evidence/gps-nearby-mobile.png)
- [Từ chối GPS](member2-evidence/gps-denied.png)
- [Trang 390 px](member2-evidence/mobile-page.png)
- [Trang khi browser offline](member2-evidence/offline-mobile.png)

## Chạy lại kiểm thử

Trong `ShortVideoTranslator.Frontend`:

```sh
npm ci
npm run lint
npm test
npx playwright install chromium
npm run test:browser
```

Linux thiếu thư viện browser có thể dùng `npx playwright install --with-deps chromium`.
Nếu máy đã có Chromium, đặt `PLAYWRIGHT_CHROMIUM_EXECUTABLE` tới file thực thi;
runner tự dùng `/usr/bin/chromium` khi có. Kết quả mới ở `artifacts/member2/`
ngoài thư mục frontend, được Git ignore. Các ảnh trong docs là bản chụp bàn giao.
Browser test tự mở server fixture tại cổng trống, tự đóng sau kiểm tra; không cần API thật.

## Phiếu kiểm thử điện thoại thật — người kiểm thử điền

Thiết bị: __________  Hệ điều hành: __________  Trình duyệt/phiên bản: __________
Ngày: __________  Commit kiểm thử: __________  Link HTTPS Cloudflare: __________

| Ca | Thao tác | Kỳ vọng | Kết quả/ảnh thực tế |
|---|---|---|---|
| M01 | Reset quyền vị trí của trang, mở link HTTPS | Trình duyệt hỏi quyền hoặc hiển thị trạng thái quyền đã có | Chưa chạy |
| M02 | Cho phép; bật định vị hệ điều hành | Có vị trí, khoảng cách và danh sách gần bạn | Chưa chạy |
| M03 | Từ chối quyền | Có hướng dẫn, vẫn chọn địa điểm thủ công | Chưa chạy |
| M04 | Tắt GPS hệ điều hành rồi thử lại | Báo lỗi/timeout, không dùng tọa độ cũ để gợi ý | Chưa chạy |
| M05 | Đi bộ đổi vị trí, lần lượt chọn 1/3/5/10 km | Danh sách và khoảng cách cập nhật; tăng bán kính mở rộng kết quả | Chưa chạy |
| M06 | Bấm Tắt định vị rồi di chuyển | Không cập nhật GPS, không tự đổi địa điểm; bật lại hoạt động | Chưa chạy |
| M07 | Chọn địa điểm/ngôn ngữ; nghe, dừng, phát lại | Đúng bài/ngôn ngữ; nghe kiểm tra âm thực tế | Chưa chạy |
| M08 | Đổi địa điểm/ngôn ngữ khi audio đang tải hoặc phát | Audio cũ dừng, không phát trễ sai bài | Chưa chạy |
| M09 | Chuẩn bị gói đầy đủ online; bật chế độ máy bay; reload | Đủ giao diện, địa điểm, ảnh, bản đồ và MP3 đã tải | Chưa chạy |
| M10 | Xoay màn hình, xem nút/chữ/ảnh/bảng | Không tràn trang, nút dễ bấm, nội dung đọc được | Chưa chạy |

Tự phát âm thanh có thể bị iOS/Android chặn cho tới thao tác người dùng đầu tiên;
ghi rõ thông báo và thử nút phát thủ công. Gửi ảnh/video không chứa tài khoản,
mật khẩu hoặc vị trí cá nhân không cần thiết. Chưa điền “Đạt” trước khi thử thật.

## Lỗi đã xử lý / giới hạn

| Vấn đề | Xử lý |
|---|---|
| Phải bấm nút mới bắt đầu GPS | Tự khởi động watcher khi trang mở, vẫn tôn trọng quyền trình duyệt |
| Không có bộ lọc gần bạn 1/3/5/10 km | Thêm danh sách có khoảng cách và sắp xếp động |
| Thông báo GPS bị thông báo chuẩn bị audio ghi đè | Tách trạng thái GPS riêng |
| Giữ vị trí hiển thị sau khi dừng | Xóa vị trí và dọn watcher/callback |
| GPS `NaN`/âm vẫn có thể qua kiểm tra độ chính xác | Kiểm tra số hữu hạn, biên tọa độ và reset debounce khi tín hiệu kém |
| Không thể chứng minh GPS thật bằng browser mock | Đánh dấu chưa chạy trong phiếu thiết bị thật |

Bản tích hợp giữ SQLite/giám sát thiết bị của main và dùng một watcher GPS chung cho danh sách gần bạn, thuyết minh và chia sẻ vị trí có đồng ý. Hai nút dừng/bật ở danh sách và Tiện ích điều khiển cùng trạng thái; đổi ngôn ngữ không tạo watcher mới.

Ảnh và JSON bên trên được cập nhật từ lượt kiểm thử bản tích hợp. Xem [hướng dẫn Git](MEMBER2-COMMIT-GUIDE.md).
