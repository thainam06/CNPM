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
