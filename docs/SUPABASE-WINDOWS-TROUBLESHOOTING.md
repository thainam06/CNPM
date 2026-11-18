# Kết nối Supabase trên Windows: xử lý theo đúng bước đang lỗi

Dùng cho máy mới clone repo, chạy PowerShell trong Terminal VS Code tại thư mục
repo (ví dụ `D:\CNPM`). `.env.docker` là file cấu hình mở trong editor; không gõ
đường dẫn file hoặc dòng `SUPABASE_CONNECTION_STRING=...` như một lệnh PowerShell.

## 1. Docker/WSL phải chạy trước

```powershell
docker info --format '{{.OSType}}'
```

Kết quả cần là `linux`. Nếu Docker báo `Wsl/Service/CreateInstance/CreateVm/E_ABORT`,
thoát Docker Desktop, lưu công việc trong các phiên WSL rồi mở PowerShell với quyền
Administrator và chạy:

```powershell
wsl --shutdown
wsl --update
```

Khởi động lại Windows và mở Docker Desktop. `wsl --shutdown` dừng tất cả phiên WSL,
không phải lệnh xóa dữ liệu. Dùng `wsl --status` và `wsl --list --verbose` để kiểm tra.
Nếu `docker-desktop` đang Running, VERSION 2 thì thông báo WSL1 chưa bật không phải
lý do để chuyển sang WSL1. Không reset Docker, unregister distro hoặc xóa volume.

Nếu Engine đã chạy mà build dừng tại `booting buildkit`, tải `moby/buildkit` và báo
EOF, kiểm tra khả năng tải đúng image mà log đang báo:

```powershell
docker pull moby/buildkit:buildx-stable-1
```

Chỉ chạy lại launcher sau khi Docker ổn định. Lỗi build ở bước này chưa chứng minh
Supabase sai mật khẩu hay dữ liệu lỗi.

## 2. Thiếu `.env.docker`

Nếu đã có cấu hình dùng được ở nơi khác, giữ nguyên và truyền `-EnvFile` với đường
 dẫn thật. Nếu đây là máy mới chưa có cấu hình, tạo file mẫu mà không ghi đè file cũ:

```powershell
if (!(Test-Path .env.docker)) { Copy-Item .env.docker.example .env.docker }
code .env.docker
```

Trong **editor**, bỏ dấu `#` trước `SUPABASE_CONNECTION_STRING` và thay tất cả
placeholder bằng Host, Port, Database, User thật từ đúng project, Connect → Session
pooler và mật khẩu database do người quản lý cấp riêng. Không dán nguyên mẫu vào
Terminal, không đoán hostname, không reset mật khẩu dùng chung để làm kiểm tra.
Mật khẩu database không phải mật khẩu đăng nhập Supabase hay mật khẩu Admin web.

Project chưa xuất hiện trên Dashboard: kiểm tra tài khoản/email và lời mời vào
organization của nhóm. Tạo project mới không cấp quyền vào database đang dùng.
Không đưa `.env.docker` hoặc connection string có password vào Git, ảnh hoặc chat.
Các giá trị Admin/JWT trong file mẫu phải được thay bằng cấu hình riêng phù hợp trước
khi khởi động web; review chỉ đọc không khởi tạo tài khoản Admin.

## 3. Build đã đạt nhưng báo `TLS`

`TLS`, stage `connect` nghĩa là bắt tay TLS thất bại; chưa xác nhận mật khẩu đúng,
chưa đọc dữ liệu. Kiểm tra hostname đúng Session pooler. Lấy đúng CA certificate
của project từ Database Settings/SSL Configuration, lưu trên máy, ví dụ:

```powershell
Test-Path D:\CNPM\supabase-ca.crt
```

Khi kết quả là True, chỉ định rõ chứng chỉ:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Start-Supabase.ps1 -ReviewOnly -IncludeConnectionInfo -CertificateFile D:\CNPM\supabase-ca.crt
```

`-CertificateFile` nhận cả đường dẫn có khoảng trắng nếu đặt trong dấu nháy kép,
và đường dẫn tương đối tính từ thư mục repo. Không có tham số thì script tìm
`supabase-ca.crt`, rồi `supabase-ca.crt.crt` trong thư mục gốc. File chỉ được mount
read-only cho ReviewOnly/CheckOnly; script truyền đường dẫn trong container cho API,
API dùng VerifyFull và chứng chỉ đã chọn. Không cần ghi lại mật khẩu để sửa đường
 dẫn CA. Nếu chỉ định file không tồn tại, script dừng trước khi build/kết nối.

Bước khởi động web đầy đủ sẽ sao chép CA công khai vào data volume và cấu hình API
sử dụng cùng chứng chỉ khi restart. Hai chế độ diagnostic không sao chép vào volume.
Script khôi phục biến môi trường CA của shell sau khi kết thúc.

Nếu vẫn TLS: kiểm tra nguồn CA, hostname và ngày giờ Windows; giữ nguyên VerifyFull.
Không dùng `Trust Server Certificate`, không tắt xác minh chữ ký/TLS để làm xanh.

## 4. Đọc kết quả và gửi đúng bằng chứng

| Mã/kết quả | Ý nghĩa và bước tiếp theo |
|---|---|
| FILE | File cấu hình/chứng chỉ thiếu hoặc không đọc được trong container |
| AUTH | Đối chiếu database user và password qua người quản lý project |
| CONNECT | Kiểm tra host/port, mạng và trạng thái project; chưa kết luận sai password |
| POOLER_PROJECT | Đối chiếu đúng host và username nguyên văn của Session pooler |
| SCHEMA / REVIEW_ISSUES | Đọc mục issues; kiểm tra đúng schema và cấu trúc, không import lại |
| REVIEW_OK | Các bước đọc/schema của báo cáo đạt; chưa chứng minh CRUD, đăng nhập hoặc MP3 |

Chỉ gửi JSON diagnostic đã kiểm tra nội dung, không gửi `.env.docker`, token,
password_hash hoặc state envelope. Mẫu ghi kết quả gửi Triết:
[RESULTS.example.md](../database/supabase-review/RESULTS.example.md).

Nếu chỉ cần xem bảng/số dòng, có thể mở **Supabase SQL Editor** và chạy SQL chỉ đọc
trong [bộ kiểm tra](../database/supabase-review/README.md) mà không dùng Docker.
Việc này vẫn cần quyền Dashboard và không chứng minh backend đã kết nối thành công.
