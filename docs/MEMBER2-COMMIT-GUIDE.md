# Đưa phần thành viên 2 lên GitHub

## Lấy bản tích hợp main

Phần dưới ghi lại cách tạo ba commit ban đầu. Sau khi bản tích hợp đã lên GitHub,
lấy main bằng các lệnh sau tại thư mục repo (lưu thay đổi riêng trước khi đổi nhánh):

```bat
git fetch origin
git switch main
git pull --ff-only origin main
```

Bản tích hợp dùng một bộ GPS chung, giữ SQLite và giám sát thiết bị hiện có.
Các lệnh tạo commit phía dưới không cần chạy lại khi chỉ muốn lấy bản mới.

Các thay đổi hiện nằm trong workspace cloud. Máy Windows của bạn không tự nhận
chúng. Tải gói `member2-changes.zip` được gửi kèm, sao lưu công việc đang sửa rồi
giải nén **nội dung** vào thư mục gốc CNPM có `compose.yaml`.
Gói chỉ chứa các file thay đổi; không chứa `.git`, `.env.docker`, tài khoản,
App_Data, node_modules hoặc dữ liệu riêng. Nếu file trùng có sửa riêng, so sánh
và ghép thay đổi trước khi ghi đè.

## 1. Dùng một checkout Git

Mở CMD tại thư mục CNPM và chạy `git status`.
Nếu báo `not a git repository`, thư mục bạn đang dùng có thể là ZIP tải từ GitHub.
Tạo một checkout riêng, **không xóa thư mục đang chạy Docker**:

```bat
cd /d C:\Users\boydy\Downloads
git clone https://github.com/thainam06/CNPM.git CNPM-member2
cd CNPM-member2
```

Sau đó giải nén bản sửa vào checkout mới. Nếu clone phiên bản mới khác mã nguồn
ban đầu, kiểm tra diff trước khi thay file; gói này dựa trên commit
`2385429` (Update Docker offline demo).

## 2. Tạo nhánh và kiểm tra

```bat
git switch -c feat/member2-gps-mobile
git status
git diff --check
git diff --stat
cd ShortVideoTranslator.Frontend
npm ci
npm run lint
npm test
npx playwright install chromium
npm run test:browser
cd ..
```

Nếu Git chưa có tên/email, cấu hình bằng **tên và email GitHub của chính bạn**:
`git config user.name "Tên của bạn"` và `git config user.email "Email GitHub của bạn"`.
Không dùng danh tính của người khác. Mỗi commit cần phản ánh phần thực sự đã làm.

## 3. Tạo ba commit theo nội dung

Chạy từ thư mục gốc CNPM, không chạy trong `scripts` hoặc `system32`.

### Commit 1 — chức năng GPS và giao diện

```bat
git add ShortVideoTranslator.Frontend/src/App.css ShortVideoTranslator.Frontend/src/App.jsx ShortVideoTranslator.Frontend/src/ExploreTools.jsx ShortVideoTranslator.Frontend/src/StartupPanel.jsx ShortVideoTranslator.Frontend/src/geofence.js ShortVideoTranslator.Frontend/src/locationWatch.js ShortVideoTranslator.Frontend/tests/locationWatch.test.js
git diff --cached --stat
git commit -m "feat: add automatic GPS and nearby place filtering"
```

### Commit 2 — kiểm thử trình duyệt

```bat
git add ShortVideoTranslator.Frontend/package.json ShortVideoTranslator.Frontend/package-lock.json ShortVideoTranslator.Frontend/tests/browser/member2.cjs
git diff --cached --stat
git commit -m "test: cover mobile GPS audio and offline flows"
```

### Commit 3 — báo cáo và minh chứng

```bat
git add docs/MEMBER2-VERIFICATION.md docs/MEMBER2-COMMIT-GUIDE.md docs/member2-evidence
git diff --cached --stat
git commit -m "docs: record member 2 validation and phone checklist"
```

Các file cần thiết đã được liệt kê cụ thể; không dùng `git add .` để tránh vô tình
đưa cấu hình hoặc dữ liệu riêng của máy lên GitHub.

## 4. Push và mở PR

```bat
git log -3 --oneline
git status
git push -u origin feat/member2-gps-mobile
```

Đăng nhập GitHub khi Git Credential Manager yêu cầu. Không gửi mật khẩu/token vào chat.
Nếu không có quyền ghi repo, nhờ chủ repo cấp quyền collaborator hoặc dùng fork.
Vào GitHub → **Compare & pull request**, chọn nhánh đích của nhóm (thường là `main`).

Tiêu đề gợi ý: `Member 2: automatic GPS, nearby discovery and mobile regression`.

Nội dung PR:

> Trang khám phá tự yêu cầu quyền GPS và hiển thị các địa điểm đã xác minh trong
> bán kính 1/3/5/10 km, cập nhật theo chuyển động. Dừng định vị dọn watcher và xóa
> vị trí hiển thị; lỗi quyền/tín hiệu có hướng dẫn riêng.
>
> Validation: lint/build, 45 unit tests và 10 browser scenarios đã qua trên
> Chromium với API/GPS/âm thanh mô phỏng. Kèm ảnh và hướng dẫn thử lại.
> GPS trên điện thoại thật, giọng TTS thực tế và gói PMTiles đầy đủ vẫn cần xác nhận.

Không đánh dấu đã kiểm thử điện thoại thật hoặc đã phát hành chỉ vì đã push.
Sau khi áp dụng mã mới vào bản Docker, chạy lại lệnh `docker compose --env-file
.env.docker up --build -d --wait --wait-timeout 240` trong thư mục/cấu hình bạn đang dùng.
Giữ volume dữ liệu; không thêm `-v` khi dừng. Đóng/mở lại tab web để service worker
nhận build mới trước khi thử trên điện thoại.
