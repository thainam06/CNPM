# Thuyết minh Quận 1 — biên soạn 18/10/2026

Nội dung đầy đủ nằm trong `ShortVideoTranslator.Data/DemoNarrations.cs`, được API địa điểm trả về và hiển thị trên website. Mỗi bài có lời dẫn, phần tìm hiểu và gợi ý quan sát. Không chép nguyên bài nguồn, không dùng nội dung quảng cáo của địa điểm khác. Giữ nhãn demo: đây là nội dung do AI hỗ trợ biên soạn để phục vụ đồ án, không phải bài thuyết minh do đơn vị quản lý phát hành.

## Nguồn đối chiếu

| Địa điểm | Dữ kiện sử dụng | Nguồn |
|---|---|---|
| Chợ Bến Thành | Khánh thành năm 1914; nguồn gốc gần bến sông; tháp đồng hồ cửa Nam và các nhóm hàng | [Ban quản lý chợ](https://benthanhmarket.vn/about/gioi-thieu-ve-cho-ben-thanh.html), [Cục Du lịch Quốc gia](https://vietnamtourism.vn/index.php/tourism/items/511), [Bảo tàng TP.HCM](https://hcmc-museum.edu.vn/luu-tru/337) |
| Dinh Độc Lập | Ngô Viết Thụ; khởi công 1962, khánh thành 1966; rèm hoa đá gợi đốt trúc; mốc 30/4/1975 và xếp hạng 2009 | [Đơn vị quản lý — kiến trúc](https://dinhdoclap.gov.vn/kien-truc-dinh-doc-lap/), [Cục Di sản văn hóa](https://dsvh.gov.vn/di-tich-lich-su-dinh-doc-lap-2937) |
| Bưu điện Trung tâm Sài Gòn | Xây dựng 1886–1891; sảnh/mái vòm; bản đồ Sài Gòn 1892 và mạng điện tín 1936 | [Vietnam Post — công trình](https://vnpost.vn/en/hoat-dong-nganh/buu-dien-trung-tam-thanh-pho-ho-chi-minh-mot-diem-den-hap-dan), [Vietnam Post — lịch sử bưu chính](https://vnpost.vn/vi/tu-hao-80-nam-truyen-thong/su-ra-doi-cua-he-thong-buu-chinh-dau-tien-o-nam-ky) |
| Phố đi bộ Nguyễn Huệ | Kênh Chợ Vải, lấp năm 1887; vị trí quảng trường và hai làn xe hai bên; mở tháng 4/2015 | [Cục Du lịch Quốc gia](https://vietnamtourism.vn/index.php/tourism/items/2905/2), [bản tin ngày mở cửa 29/4/2015](https://tuoitre.vn/plo/pho-di-bo-nguyen-hue-ngay-dau-mo-cua-109335183.htm) |
| Nhà hát Thành phố | Xây từ 1898, khánh thành 1/1/1900; trang trí mặt tiền; vai trò biểu diễn | [Cơ sở dữ liệu ngành du lịch](https://csdl.vietnamtourism.gov.vn/dest/?item=381), [Cổng Chính phủ — TP.HCM](https://tphcm.chinhphu.vn/sai-gon-bien-doi-va-hinh-thanh-mot-thanh-pho-theo-kieu-phuong-tay-1014553.htm) |

Các lời mời quan sát, đi chậm, hỏi giá hoặc kiểm tra vé là hướng dẫn do người biên soạn viết thêm, không được trình bày như quy định do nguồn ban hành. Bài Nhà hát không hứa mở cửa tham quan nội thất. Không đưa giá vé, giờ mở cửa, lịch diễn, cam kết đài phun nước/Wi-Fi luôn hoạt động hoặc tên tác giả kiến trúc còn có nguồn khác nhau vào lời đọc. Không thay tọa độ, địa chỉ, hình ảnh hay quyền sở hữu.

## Sáu ngôn ngữ và cache

- Có nguyên bản Việt và bản dịch Anh, Trung giản thể, Nhật, Hàn, Pháp cho đúng năm bài này. Bản dịch thực sự chuyển nội dung sang ngôn ngữ đích; không đưa tiếng Việt cho giọng nước ngoài đọc.
- Provider `SmartTourDraft`, `isMachineTranslation=true`: bản nháp có AI hỗ trợ, chưa được chuyên gia ngôn ngữ hiệu đính. Website hiển thị đúng nhãn này; không giả là phản hồi MyMemory hay bản đã được Admin duyệt.
- `TranslationService` chỉ dùng gói mẫu khi **toàn bộ chuỗi nguồn tiếng Việt khớp chính xác**. Nếu sửa nguồn, dùng bản dịch đã duyệt cho nguồn mới hoặc MyMemory như trước; thiếu dịch vụ sẽ báo chưa sẵn sàng. Bản đã được Admin duyệt vẫn được ưu tiên.
- Gói mẫu không cần khóa dịch và không gọi MyMemory. Cờ `Translation:Enabled=false` tắt dịch từ xa; nguyên bản và bản soạn sẵn vẫn đọc được. Edge-TTS tạo MP3 mới vẫn cần Internet.
- Khóa MP3/cache gồm nguồn và ngôn ngữ. Sửa nội dung không dùng lại MP3 một câu cũ hoặc bản dịch tiếng Anh đã duyệt cho nguồn cũ. Dữ liệu cũ được giữ; manifest công khai chỉ trả phiên bản đúng nguồn hiện tại.
- Khi cần hiệu đính bản ngoại ngữ, dùng mục Bản dịch thuyết minh trong Admin để lưu nháp/duyệt và cấp revision mới. Không sửa riêng chuỗi bản dịch trong file rồi kỳ vọng MP3 cùng khóa v1 tự đổi.
- Migration v6 chỉ nâng cấp năm đoạn mặc định chưa bị sửa, có nhãn demo, do hệ thống quản lý và thuộc Quận 1. Giữ ảnh, vị trí, trạng thái ẩn/công khai; không ghi đè nội dung thành viên/chủ quán đã biên tập. Chạy lại không ghi lặp hoặc tự cấp lại MP3 cũ.

Kết quả build, MP3 thật và thử trình duyệt ghi tại `docs/VERIFICATION.md`. Nguồn ảnh/địa chỉ/tọa độ tiếp tục ghi riêng ở `DISTRICT1-SOURCES.md`.

## MP3 mới đã kiểm tra

Đã tạo 30 file qua Edge-TTS thật, 5 tác vụ đều completed 6/6. Kiểm tra chữ ký nguồn/ngôn ngữ/provider trong metadata, độ dài file, SHA-256 và giải mã thời lượng bằng ffprobe. Bản Việt dùng vi-VN-HoaiMyNeural; không dùng speechSynthesis.

| Bài Việt | Thời lượng đo được |
|---|---|
| Chợ Bến Thành | 56,352 giây |
| Dinh Độc Lập | 59,976 giây |
| Bưu điện Trung tâm Sài Gòn | 59,880 giây |
| Phố đi bộ Nguyễn Huệ | 59,544 giây |
| Nhà hát Thành phố | 59,424 giây |

MP3 và metadata trong App_Data/audio bị Git ignore, không commit; cache được tạo lại từ nội dung seed khi chuyển máy. Nhật ký kiểm tra local: artifacts/narration-content-audio-check.json (ignored). Thời lượng các bản ngoại ngữ khác nhau; không coi kiểm tra giải mã là kiểm định chất lượng dịch của chuyên gia.
