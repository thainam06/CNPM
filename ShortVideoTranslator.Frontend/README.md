# React + Vite

Ứng dụng frontend **SmartTour Guide — Quận 1**. Chạy `npm ci`, sau đó `npm run dev` tại thư mục này. Vite dùng cổng 5173 và proxy `/api` tới backend cổng 5132. Hướng dẫn đầy đủ, API và giới hạn dịch/TTS nằm trong [SMARTTOUR-DEMO.md](../docs/SMARTTOUR-DEMO.md).

Kiểm tra: `npm run build` và `npm run lint`. Cấu hình mẫu: `.env.example`. Nội dung dưới đây là tài liệu gốc của template Vite.

This template provides a minimal setup to get React working in Vite with HMR and some Oxlint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Oxc](https://oxc.rs)
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/)

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the Oxlint configuration

If you are developing a production application, we recommend using TypeScript with type-aware lint rules enabled. Check out the [TS template](https://github.com/vitejs/vite/tree/main/packages/create-vite/template-react-ts) for information on how to integrate TypeScript and Oxlint's TypeScript related rules in your project.
