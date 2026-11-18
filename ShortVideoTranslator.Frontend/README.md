# SmartTour Guide — Frontend

Ứng dụng React + Vite cho website khám phá Quận 1 và thuyết minh đa ngôn ngữ.

- Cài thư viện: `npm ci`.
- Chạy phát triển: `npm run dev` tại thư mục này; cổng mặc định 5173.
- Vite chuyển `/api` đến Backend cổng 5132. Khi dùng Backend Docker cổng 8080, đặt biến môi trường `API_PROXY_TARGET=http://localhost:8080` trước khi chạy Vite.
- Cấu hình mẫu: `.env.example`.
- Trang người dùng: `/`. Quản trị: `/admin`. Cổng chủ quán: `/owner`.

Kiểm tra: `npm test`, `npm run build`, `npm run lint`.
Kiểm tra trình duyệt: `npm run test:browser` (cần Chromium của Playwright).

Hướng dẫn dự án: [SMARTTOUR-DEMO.md](../docs/SMARTTOUR-DEMO.md).
Cấu hình Docker/Supabase: [DOCKER-SUPABASE.md](../docs/DOCKER-SUPABASE.md).
Không lưu mật khẩu hoặc connection string trong source frontend hay biến `VITE_*`.

## Tìm source

- `src/pages/`: `UngDung.jsx` (khách), `CongQuanTri.jsx` (Admin/chủ quán).
- `src/components/`: danh sách, bản đồ, ảnh, nội dung thuyết minh và thành phần quản trị.
- `src/services/`: gọi API, cache offline, GPS, thống kê và chuẩn bị ngôn ngữ.
- `src/utils/`: hàm thuần, xác thực đầu vào, khoảng cách và chính sách lưu trữ.
- `src/hooks/`: hook định vị dùng chung.
- `src/data/`: nguồn ảnh, từ điển và `ui-bundles.json` dùng chung với Backend.
- `src/styles/`: CSS. `index.css` ở gốc `src` là nền tảng hiển thị toàn ứng dụng.
- `src/main.jsx`, `src/GocUngDung.jsx`: điểm khởi động và chọn trang.

Giữ `package.json` và `package-lock.json` để npm và CI cài đúng thư viện.
Giữ `.oxlintrc.json` để kiểm tra code. Giữ `src/data/ui-bundles.json` vì React
và Business cùng dùng nó; đường dẫn được khai báo trong `docker/api.Dockerfile`
và `ShortVideoTranslator.Business.csproj`.
