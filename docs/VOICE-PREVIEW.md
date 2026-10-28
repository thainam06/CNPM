# Nghe thử giọng đọc riêng cho Admin

Trang mới: `http://localhost:5173/voice-preview.html` khi chạy bằng Vite,
hoặc `/voice-preview.html` trên website đã triển khai. Đăng nhập Admin ở cùng
website trước khi mở trang; tài khoản cần quyền `poi:update`.

Chọn một trong sáu ngôn ngữ và giọng nam/nữ, nhập văn bản bằng ngôn ngữ đó,
bấm **Nghe thử**, rồi bấm phát audio. Nút **Dừng**, đổi nội dung, đổi giọng hoặc
đổi ngôn ngữ sẽ hủy yêu cầu đang chờ và bỏ audio cũ. Trang không tự dịch văn bản.

API riêng: `GET /api/admin/voice-preview` lấy danh sách lựa chọn;
`POST /api/admin/voice-preview` nhận `text`, `language`, `voice` và trả MP3.
Backend kiểm tra ngôn ngữ/giọng khớp nhau, giới hạn 3000 ký tự, áp dụng phân quyền,
CSRF cho cookie và rate limit hiện có. Không lưu vào database hoặc cache audio.
File tạm của tiến trình được dọn sau khi trả dữ liệu; phản hồi đặt `no-store`.

Tính năng này chỉ nghe thử. Nó không thay đổi giọng mặc định của nội dung công
khai, bản nháp, bản đã duyệt hay MP3 hiện có. Không có thay đổi schema database.

Cần Python và `edge-tts` cùng Internet như công cụ thuyết minh hiện tại.
Tên giọng phụ thuộc dịch vụ Edge-TTS; nếu dịch vụ không hoạt động, trang báo lỗi
và không tác động đến nội dung đã công khai.

Kiểm thử dùng output giả để xác nhận chọn đúng giọng, phân quyền và không ghi
bản nháp/công khai. Chúng không chứng minh chất lượng phát âm hoặc dịch vụ online.

Chạy kiểm thử backend với `dotnet test ShortVideoTranslator.Tests --filter FullyQualifiedName~VoicePreview`.
Trong thư mục frontend, chạy `node tests/browser/voicePreview.cjs` sau khi đã cài
dependencies và Chromium cho Playwright. Test trang riêng không cần backend thật.

Code chức năng: `ShortVideoTranslator.Business/VoicePreview.cs`, API tại
`ShortVideoTranslator.Api/Controllers/VoicePreviewController.cs`, giao diện tại
`ShortVideoTranslator.Frontend/public/voice-preview.html`.
