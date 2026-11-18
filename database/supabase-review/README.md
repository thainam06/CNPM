# Bộ file kiểm tra Supabase để gửi Triết

Soạn ngày 18/11/2026 từ SQL và adapter trong repo. Lượt này **chưa kết nối
Supabase online**; không coi nội dung tài liệu là kết quả kiểm chứng mới.

## 1. Tên đúng trong repo

Schema là **smarttour_demo**, có 6 bảng:

| Tên trong tin nhắn | Tên SQL trong repo |
|---|---|
| user | users |
| port (cần nhóm xác nhận ý nghĩa) | places là bảng địa điểm mà backend dùng |
| place_images | place_images |
| place_narrations | place_narrations |
| visitor_sessions | visitor_sessions |
| activity_logs | activity_logs |

Không tạo thêm bảng `user`/`port`, đổi tên hoặc chạy lại migration chỉ để khớp tin nhắn.
Nguồn: `database/postgres/002_smarttour_compact.sql`,
`005_six_table_backend.sql`, `ShortVideoTranslator.Data/PostgresPlatformStore.cs`
và `PostgresStateCodec.cs`. Thiết kế gồm 44 cột. Migration 005 yêu cầu FK deferrable.

## 2. Cách xem trên Supabase

1. Mở https://supabase.com/dashboard, chọn đúng project đã được cấp quyền.
2. Table Editor: đổi schema từ `public` sang `smarttour_demo`, chọn từng bảng.
3. SQL Editor: chạy lần lượt các file dưới bằng tài khoản có quyền xem schema.

| File | Kết quả cần xem |
|---|---|
| `01_check_schema.sql` | 6 bảng FOUND, cấu trúc cột, PK/FK/check, RLS và index |
| `02_view_data.sql` | Số dòng hiện tại, mẫu dữ liệu có chọn lọc, ảnh/narration, thống kê |
| `03_check_integrity.sql` | Bản ghi mồ côi, consent, ngôn ngữ, bản duyệt/ảnh bìa trùng, tọa độ và thời gian |

Các file chỉ có SELECT, không tạo bảng, ghi dữ liệu hay thay quyền. Không dùng
chúng thay cho migration. MISSING_OR_NOT_VISIBLE có thể do thiếu bảng **hoặc thiếu
quyền**. Nếu 01 báo thiếu cột/bảng thì dừng trước 02/03 và kiểm tra lại schema.
Các query hoạt động trong phạm vi quyền/RLS của người chạy; RLS bật không tự chứng
minh phân quyền đúng. Không tắt RLS hoặc cấp PUBLIC để làm query chạy.

Kết quả violations phải là 0; mỗi giá trị khác 0 cần điều tra trước khi sửa. Backend
đã import thường có đúng một dòng `system:platform-state:v1`; DB chỉ mới tạo schema
có thể chưa có. Không xóa dòng này: adapter lưu workflow phụ trong details. Không
xuất `users.password_hash`, users.settings hoặc activity_logs.details cho nhóm.

`visitor_sessions` là presence 45 giây, không phải analytics có consent trong cửa sổ
5 phút. Nhiều tab cùng browser không đồng nghĩa nhiều thiết bị. Query nhóm thông tin
thiết bị dùng đúng cấu trúc codec `device_info.descriptor`, không giả định các key phẳng.

`audio_path` có giá trị chưa chứng minh file MP3 tồn tại hoặc phát đúng. Adapter hiện
có thể giữ audio ngoài SQL; trường audio_path rỗng không tự chứng minh TTS bị hỏng.
Ảnh bìa có thể nằm trong places.settings, khác ảnh gallery trong place_images.

## 3. Session pooler và cách chạy thử ứng dụng

Copy `SESSION-POOLER.example.txt` thành bản bàn giao riêng, điền Host/Port/Database/User
nguyên văn từ **Connect → Session pooler**. Không suy ra thông tin kết nối từ tên repo
hoặc ví dụ. Không điền mật khẩu vào file được commit. Người quản trị gửi mật khẩu riêng;
`.env.docker` chứa connection string chỉ giữ local và phải bị Git ignore.

Hướng dẫn chạy có sẵn tại [HUONG-DAN-XEM-SUPABASE.md](../../HUONG-DAN-XEM-SUPABASE.md)
và [DOCKER-SUPABASE.md](../../docs/DOCKER-SUPABASE.md). Khi người vận hành muốn kiểm tra
online, từ thư mục repo trên máy có Docker, `.env.docker` và CA certificate hợp lệ:

```powershell
# Kiểm tra kết nối trước, không import dữ liệu.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Start-Supabase.ps1 -CheckOnly

# Chỉ chạy bước này khi chủ động muốn khởi động ứng dụng.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Start-Supabase.ps1
```

Sau đó kiểm tra provider ở `/api/health` phải là `postgresql`,
`/api/health/ready` phải ready và `/api/places` phải khớp dữ liệu SQL. Đăng nhập/CRUD
và ghi analytics cần thử trên môi trường riêng, có quyền của người quản trị; không
suy ra thành công chỉ vì SELECT chạy được. Lượt soạn file này không chạy các lệnh trên.

Không chạy `001`, `003`, `004`, import/reset hoặc DROP/TRUNCATE để kiểm tra đọc.
Giữ TLS VerifyFull và CA hợp lệ; không tắt xác minh TLS khi gặp lỗi.

## 4. Phân biệt báo cáo cũ và kết quả mới

`HUONG-DAN-XEM-SUPABASE.md` ghi lần kiểm tra **15/11/2026**: kết nối OK, 8 places,
8 images, 8 narrations, 1 user, 10 activity logs, 0 sessions. Đây là số liệu lịch sử
trong repo, không phải số liệu online đã được kiểm tra lại ngày 18/11.
Tài liệu thiết kế ngày 29/10 còn một số mô tả trước khi có adapter; ưu tiên đọc SQL,
adapter hiện tại và ngày kiểm chứng của từng báo cáo.

## 5. Mẫu trả lời Triết sau khi thực sự chạy kiểm tra

> Schema: smarttour_demo. Tên bảng dùng users và places (đối chiếu lại chữ port).
> Ngày kiểm tra: ...; đủ 6 bảng: ...; số dòng từng bảng: ...;
> integrity/foreign keys: ...; PostgreSQL readiness/API: ...;
> đăng nhập và thao tác ghi: đã thử/chưa thử ...
> Host/Port/Database/User: theo bản Session pooler đính kèm.
> Mật khẩu gửi riêng, không nằm trong Git.

Chỉ điền “đạt” sau khi có kết quả chạy thực tế. Không gửi ảnh bảng chứa bí mật hoặc
bản ghi riêng tư; các SELECT mẫu đã bỏ password/token và nội dung workflow phụ.

## 6. Xuất báo cáo tự động cho nhóm (chỉ đọc)

Đã thêm chế độ độc lập, chạy trước bootstrap/worker của API:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Start-Supabase.ps1 -ReviewOnly
# Chỉ bật tùy chọn này khi muốn gửi thêm Host/Port/Database/User cho đúng thành viên nhóm:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Start-Supabase.ps1 -ReviewOnly -IncludeConnectionInfo
```

Không kết hợp `-ReviewOnly` và `-CheckOnly`. Review không chạy import, không chép CA
vào volume, không bật web/worker. CA ở thư mục gốc (nếu có) được mount read-only vào
`/app/App_Data/supabase-ca.crt`; connection string phải trỏ đúng đường dẫn certificate.
Script vẫn build API, có thể cần mạng tải image/package. Lỗi credentials/TLS/network phải
được xử lý bằng cấu hình đúng; không chuyển sang SQLite để báo thành công.

Nếu chạy .NET trực tiếp với cấu hình private đã có ở appsettings.Local.json,
Storage__PostgresConnectionString hoặc Storage:PostgresConnectionFile:

```bash
dotnet run --project ShortVideoTranslator.Api -c Release -- \
  --ReviewPostgres=true --ImportPostgres=false
```

Không truyền password trong command line. Có thể thêm `--IncludeConnectionInfo=true`
để hiện thông tin pooler không chứa password. Mặc định thông tin kết nối bị bỏ khỏi báo cáo.
Chương trình dùng transaction PostgreSQL **READ ONLY**, snapshot RepeatableRead,
rollback khi kết thúc, timeout mỗi lệnh 15 giây. Không gọi write-probe của `-CheckOnly`
(vốn thực hiện UPDATE kiểm tra rồi rollback), không khởi động app.

Báo cáo JSON có các section:
- `schema-1..5`: bảng, cột/kiểu, constraints, RLS và index.
- `counts-1`: số dòng 6 bảng.
- `integrity-1..4`: lỗi, FK, metadata backend và cảnh báo nội dung.
- `transaction-1`: transaction_read_only phải là `on`.

Exit 0 / REVIEW_OK chỉ xác nhận các bước kiểm tra cấu trúc/đọc đã đạt, không chứng minh
đăng nhập, CRUD, quyền ghi, MP3 hay toàn bộ ứng dụng chạy được. Exit 2 / REVIEW_ISSUES
nghĩa là có cấu trúc/dữ liệu cần xem lại; bảng mới chưa import sẽ thiếu state envelope.
Lỗi cấu hình/kết nối trả mã an toàn, không in exception chứa password/dữ liệu server.
Báo cáo không xuất mật khẩu, tài khoản, token hoặc nội dung JSONB envelope; phần nội dung
chỉ có ID/tên địa điểm và cờ cảnh báo. Kiểm tra quyền chia sẻ trước khi gửi báo cáo ra ngoài nhóm.

## 7. Bằng chứng lượt thay đổi 18/11/2026

- Build Release: đạt, không warning/error.
- 144 backend tests đạt, **0 skipped**, gồm PostgreSQL integration trên container tạm.
- CLI ReviewPostgres chạy thực tế trên database thử, báo READ ONLY và không lộ credential.
- 3 file SQL đã thực thi bằng PostgreSQL 17 trên dữ liệu fixture; không chỉ kiểm tra cú pháp.
- Kiểm tra cấu hình/schema sai, sai kiểu cột, kho chưa import, snapshot không đổi sau review.
- PowerShell launcher kiểm tra cả thành công/thất bại của review; không khởi động web/worker.
- Chưa kết nối lại project Supabase online: workspace không có env/connection file và CA.
  Host/Port/Database/User trong template vẫn để trống; không lấy ví dụ làm thông tin thật.

GitHub Actions có job `supabase-review` chạy PostgreSQL riêng và lưu JSON review fixture.
Artifact này là bằng chứng local/CI, không phải báo cáo dữ liệu Supabase production.
