# Chuyển schema Supabase 15 bảng về đúng 6 bảng cho Admin

## Phạm vi đã triển khai

Backend dùng `smarttour_demo` với 6 bảng / 44 cột:
`users`, `places`, `place_images`, `place_narrations`, `visitor_sessions`, `activity_logs`.
Nguồn 15 bảng trong ảnh nhóm phù hợp thiết kế mở rộng tại migration **004**. Không
chạy lại 004. Công cụ mới kiểm tra schema thật trước; nếu đã là 6 bảng thì trả
`ALREADY_COMPACT` mà không chuyển đổi lại.

Công cụ **không xóa 9 bảng**, không DROP/recreate dữ liệu thật. Khi đủ điều kiện,
trong một transaction nó đổi tên toàn bộ schema nguồn thành
`smarttour_demo_archive15`, tạo lại `smarttour_demo` theo 002/005, copy đúng 44 cột,
kiểm tra dữ liệu và codec trước COMMIT. ID, password hash, security stamp, roles và
các collection backend trong snapshot được giữ. Hai identity sequence được đặt sau
ID lớn nhất ở thời điểm cutover. Schema đích có 5 FK deferrable và RLS bật.
Archive giữ **nguyên cả 15 bảng, mọi cột và bản ghi**, kể cả cột mở rộng và quan hệ
nội bộ; archive không thay thế backup ngoài database.

## Điều kiện được hỗ trợ — đọc trước khi apply

1. Nguồn đúng 15 bảng của 004, với 44 cột lõi có đúng kiểu backend. Không tự đoán
   mapping cho schema khác hoặc dữ liệu UUID/kiểu khác.
2. Database có dữ liệu phải có snapshot codec hợp lệ tại ID
   `system:platform-state:v1`. Không có snapshot mà core đã có bản ghi thì dừng
   `SNAPSHOT_REQUIRED`; không được bootstrap/import đè để vượt kiểm tra. Các ID của
   cả 6 bảng lõi phải round-trip qua codec đầy đủ; narration/ảnh/log nhập tay mà codec
   không biểu diễn được cũng sẽ chặn chuyển đổi, tránh bị bỏ sau lần Admin ghi tiếp theo.
3. Snapshot compact phải được chủ dữ liệu xác nhận là nguồn vận hành đúng.
   Các cột mở rộng của 004 được giữ trong archive, **không tự ghi đè** metadata compact.
   Chạy SQL chỉ đọc `database/postgres/007_review_15_table_data.sql` sau khi xác nhận
   schema 004 để đếm khác biệt ở một số trường tiêu biểu; query này không đối chiếu đủ mọi
   cột mở rộng hay mọi nghiệp vụ. Số khác biệt > 0 cần đối chiếu nguyên nhân;
   không mặc định các giá trị đó đều chỉ là default/backfill.
4. Tám bảng workflow mở rộng phải rỗng: menu_items, owner_requests, place_proposals,
   notifications, audio_assets, audio_tasks, audit_logs, runtime_sessions.
   Nếu có bản ghi, công cụ dừng `MAPPING_REQUIRED`; phải ánh xạ rõ từng nghiệp vụ trước.
   `roles` chỉ được có tên role seed từ users như 004, permissions rỗng/is_system=false.
   Quyền đang vận hành được giữ từ snapshot backend, không tự cấp quyền từ bảng roles.
5. Người thực thi phải có toàn quyền đọc nguồn, không bị RLS lọc bớt dòng. Công cụ
   dừng `VISIBILITY_REQUIRED` để tránh nhầm dữ liệu bị ẩn thành database rỗng. Không
   tắt RLS hoặc cấp quyền PUBLIC để vượt kiểm tra.
6. Không có FK/view ở schema khác phụ thuộc nguồn. Chủ dữ liệu phải kiểm tra thêm
   function/client/job dùng tên schema cứng; công cụ không phân tích SQL động bên ngoài.
7. Có backup ngoài database đã thử khôi phục, đủ dung lượng giữ archive và bản copy
   6 bảng, quyền tạo/đổi schema, **đã dừng mọi
   API/worker/client ghi dữ liệu**. Các writer đang chờ trên bảng cũ cũng phải dừng,
   tránh việc chúng ghi vào archive sau cutover. Lock/transaction không thay việc này.

Nguồn 15 bảng hoàn toàn rỗng được chuyển về 6 bảng rỗng. Công cụ không tạo tài khoản,
không tạo snapshot giả; phải import nguồn có thẩm quyền bằng quy trình riêng trước
khi khởi động backend. Có schema đúng không đồng nghĩa có dữ liệu đủ để Admin chạy.

## 1. Chuẩn bị và lấy kế hoạch — chỉ đọc

Giữ `.env.docker` private và CA certificate như hướng dẫn hiện có. Không gửi mật khẩu
vào Git/chat. Nếu clone cũ báo nhánh diverged sau các lần sửa lịch sử Git, giữ mọi thay
đổi/file cấu hình và đồng bộ với người tích hợp trước; không force-push lịch sử cũ trở lại.

Từ thư mục repo trong PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Migrate-Supabase.ps1 -CertificateFile D:\CNPM\supabase-ca.crt
```

`-CertificateFile` dùng đường dẫn CA thật, có thể bỏ nếu đã đặt file CA ở thư mục gốc
với tên được launcher hỗ trợ, hoặc connection string đã có trust hợp lệ. Luôn giữ
VerifyFull; không tắt TLS. Dùng `-EnvFile` nếu env nằm ở vị trí khác.

Mặc định là PLAN: SELECT trong transaction READ ONLY, không chạy web/worker,
không import, không đổi schema hoặc tài khoản. Đọc JSON: `code`, `initialized`,
`counts`, `blockers`, `warnings`. `PLAN_READY` chưa có nghĩa đã chuyển đổi.

| Code | Hành động |
|---|---|
| ALREADY_COMPACT | Đã đúng 6 bảng/cột; chạy ReviewOnly và kiểm thử app, không migrate lại |
| UNSUPPORTED_SCHEMA / COLUMN_MISMATCH | Gửi cấu trúc an toàn, viết mapping/migration đúng nguồn |
| VISIBILITY_REQUIRED | Nhờ owner kiểm tra bằng role nhìn thấy toàn bộ nguồn; không tắt RLS |
| SNAPSHOT_REQUIRED | Không có snapshot tương thích; đối chiếu nguồn, không nhập đè |
| MAPPING_REQUIRED | Xử lý các workflow rows/role/quyền/phụ thuộc được liệt kê |
| PLAN_READY | Kế hoạch có thể áp dụng sau backup, dừng writer và xác nhận nguồn |
| BACKUP_REQUIRED / SNAPSHOT_CONFIRMATION_REQUIRED | Chưa đủ xác nhận; không có thay đổi |
| MIGRATED | Transaction đã commit; tiếp tục verify trước khi cho khách truy cập |

## 2. Backup, đối chiếu và maintenance

Người quản trị tạo backup private bằng `pg_dump` hoặc công cụ backup PostgreSQL được
phê duyệt, gồm schema, data và các phụ thuộc cần thiết, rồi thử restore trên database
riêng. PostgreSQL credentials dùng prompt/config private; không đưa password vào
command line hoặc ghi vào báo cáo. Backup trong cùng database không bảo vệ khỏi mất
cả project. Free plan không đồng nghĩa chắc chắn có backup Dashboard sẵn để phục hồi.

Đối chiếu dữ liệu expanded với snapshot compact, xem counts ở 007 và các blocker.
Dừng API và worker đang ghi (với đúng Compose context của máy vận hành). Không dùng
`down -v`. Nếu chưa biết snapshot nào đúng hoặc chưa có backup, dừng tại PLAN.

## 3. Apply có xác nhận

Chỉ sau khi đã thực hiện đầy đủ mục 2:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Migrate-Supabase.ps1 -Apply -BackupConfirmed -AcceptCompactSnapshot -CertificateFile D:\CNPM\supabase-ca.crt
```

`-BackupConfirmed` xác nhận cả backup và writer đã dừng; không tự làm backup/dừng
server. `-AcceptCompactSnapshot` xác nhận dùng dữ liệu compact sẵn có làm nguồn vận
hành, không phải tự động ánh xạ mọi dữ liệu 15 bảng. Công cụ từ chối ghi đè archive
đã tồn tại. Khi copy, constraint hoặc kiểm chứng thất bại, toàn bộ DDL/data rollback;
không xóa schema/tables để thử lại. Lock timeout/SQLSTATE 55P03 cần kiểm tra writer.

CLI .NET tương đương, dùng cấu hình private mà backend hiện có (không truyền password):

```powershell
dotnet run --project ShortVideoTranslator.Api -c Release -- --MigratePostgresSixTables=true
# Thêm các cờ dưới chỉ khi chủ động apply sau backup/maintenance:
# --ApplyPostgresMigration=true --BackupConfirmed=true --AcceptCompactSnapshot=true
```

Với profile `compose.supabase.file.yaml`, có thể dùng đúng Compose context file đó
và `run --rm --no-deps api --MigratePostgresSixTables=true` thay cho launcher env;
phải mount CA và cấu hình đường dẫn trong container đúng. Không dùng cả hai profile
connection-string/file cùng lúc rồi đoán cấu hình nào thắng.

## 4. Verify và mở lại ứng dụng

Chưa khởi động web sau `MIGRATED` cho tới khi kiểm tra:

- `Start-Supabase.ps1 -ReviewOnly`: 6 bảng, 44 cột, đúng PK/FK/type, state envelope hợp lệ.
- So sánh số dòng/ID tài khoản, địa điểm, ảnh/narration và checksum file MP3 với backup.
  Chứng chỉ/ảnh/MP3/DataProtection-Keys vẫn thuộc filesystem/volume, không nằm trong migration SQL.
- Chạy app bằng đúng Postgres context; health storage phải `postgresql`, readiness ready.
- Đăng nhập tài khoản cũ, CRUD địa điểm trên kho thử được phép, restart và đọc lại.
- Kiểm tra permissions, consent/log và source language. Không xuất users.settings,
  password_hash hoặc nội dung state envelope ra frontend/báo cáo.

Nếu role migration khác role backend, owner phải cấp quyền backend tối thiểu phù hợp
trên schema/bảng/sequence mới và kiểm tra RLS; không cấp PUBLIC để làm app chạy.
Sau verify mới mở lại từng writer. Không để instance cũ còn ghi vào schema archive.
Dùng [RESULTS.example.md](../database/supabase-review/RESULTS.example.md) ghi kết quả
thật và phần chưa kiểm tra. `ReviewOnly`/migration thành công không chứng minh toàn bộ UC đạt.

## 5. Rollback có giữ cả dữ liệu sau cutover

Nếu apply lỗi trước COMMIT, transaction tự rollback. Nếu đã COMMIT, dừng mọi writer,
backup cả hai schema và đối chiếu các ghi mới trước khi rollback. Người quản trị có
thể đổi tên schema thay vì DROP (tên `smarttour_demo_after_cutover` phải chưa tồn tại):

```sql
BEGIN;
ALTER SCHEMA smarttour_demo RENAME TO smarttour_demo_after_cutover;
ALTER SCHEMA smarttour_demo_archive15 RENAME TO smarttour_demo;
COMMIT;
```

Cách này giữ cả bản 15 bảng và bản 6 bảng sau cutover; nó không tự hợp nhất dữ liệu mới.
Backend 6 bảng không tương thích schema 15 bảng đã phục hồi. Cần dùng lại môi trường
vận hành trước đó hoặc thực hiện đối chiếu/mapping, không kết luận rollback đã làm app
sẵn sàng. Không tự xóa archive, volume hoặc backup sau khi chuyển đổi.

## Kiểm chứng trong repo

Các integration test dùng PostgreSQL tạm, mô phỏng 002/005 → import fixture → 004:
chuyển về 6 bảng/44 cột, giữ nguyên archive/snapshot/password, sequence/FK, Admin
login/CRUD/restart; rollback sau lỗi constraint; chặn workflow rows chưa mapping,
external dependencies, archive trùng tên, sai type/snapshot; và giữ writer đã commit
trong lúc chờ lock. Đây không phải kết quả chạy trên Supabase online của nhóm.
