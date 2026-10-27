# Thiết kế Audit Log

Ngày: 2026-10-27

## Mục đích

Bộ phận vận hành cần xem và quản lý thao tác của người dùng trên hệ thống: request nào xảy ra lỗi, lỗi gì, message gì, và request đó đã làm thay đổi dữ liệu nào. Audit log phải không làm chậm request, và nội dung request/response phải được mã hóa khi lưu.

## Mục tiêu

- Ghi lại mỗi HTTP request đi qua API thành một bản ghi audit duy nhất.
- Bản ghi có đủ: status, error_message, request body, response body, và các thay đổi entity do request đó gây ra.
- Ghi bất đồng bộ: middleware là publisher, đẩy vào `System.Threading.Channels.Channel`; một `BackgroundService` là consumer, đọc từ channel và ghi xuống DB.
- Request body, response body và danh sách thay đổi entity được mã hóa trước khi lưu.
- Bảng nằm trong schema `audit`.
- Hai API đọc: danh sách có phân trang + lọc theo `status` / `AuditType`, và chi tiết theo id.

## Không phải mục tiêu (lần này)

- API trả request/response/changes đã giải mã. Người dùng sẽ tự làm riêng sau.
- Lưu header của request/response.
- Loại trừ endpoint khỏi audit (swagger, health...). Hiện chưa có endpoint nghiệp vụ nào, và người dùng muốn ghi hết.
- Lưu stack trace của exception.
- Ghi audit cho thao tác DB ngoài HTTP request (background job...).
- Xoay khóa mã hóa (chỉ chừa chỗ trong định dạng, xem bên dưới).

## Điều gây thất bại (anti-goals)

- Audit làm chậm hoặc làm lỗi request thật. Lỗi ở audit chỉ được log, không được ảnh hưởng response.
- Request/response/changes nằm dạng plaintext trong DB.
- Ghi đệ quy: bản thân bảng audit bị ghi vào audit.

## Ràng buộc từ code hiện có

- .NET 10, EF Core + **PostgreSQL** (`UseNpgsql`). (CLAUDE.md nói SQL Server là cũ.)
- Giữ chiều phụ thuộc Clean Architecture: WebAPI → Application → Domain; Infrastructure/Persistence cài đặt abstraction của Application/Domain.
- Bám mẫu `WebhookEvent`: entity kế thừa `EntityBase<Guid>` với `Guid.CreateVersion7()`, cấu hình trong `Persistence/Configurations` (`ToTable("...", "<schema>")`), repository qua `RepositoryBase`.
- Lỗi có hai kiểu: exception đi qua `GlobalExceptionHandler` (trả ProblemDetails), và `Result.Failure(statusCode, message)` trả 4xx bình thường không throw.
- `ApplicationDbContext.SaveChangesAsync` đã tự xử lý domain events và audit field; phần thu thay đổi entity thêm vào đây.
- Lấy user qua `ICurrentUserService`.

## Các quyết định của người dùng

1. Schema: `audit`.
2. `AuditType` có hai giá trị: `Activity`, `Exception`.
3. Mỗi request là **một** bản ghi. Thay đổi entity được gộp vào bản ghi của request đó (cột `Changes`), không tách dòng riêng.
4. `UserId` và `Username` cho phép null.
5. Chỉ lưu **body** của request (không lưu header). Lưu hết mọi request, không loại trừ.
6. Request/response mã hóa khi lưu. API trả nội dung đã giải mã sẽ làm sau; hiện tại API đọc không giải mã.
7. Hai API: danh sách (phân trang, lọc `status`, `AuditType`) và chi tiết theo id.

## Thiết kế (phần do tôi chọn, người dùng chưa duyệt từng điểm)

### Dữ liệu: `audit.AuditLogs`

| Cột | Ghi chú |
|---|---|
| `Id` | Guid v7 |
| `AuditType` | `Activity` / `Exception`, lưu dạng chuỗi |
| `StatusCode` | HTTP status code cuối cùng (int) |
| `ErrorMessage` | plaintext, nullable, để lọc/đọc nhanh |
| `HttpMethod`, `Path` | plaintext |
| `UserId` (Guid?), `Username` (string?) | nullable |
| `DurationMs` | thời gian xử lý |
| `RequestBody`, `ResponseBody`, `Changes` | mã hóa, nullable |
| `IsRequestTruncated`, `IsResponseTruncated` | bool, plaintext |
| `CreatedAt` | DateTimeOffset UTC |

Index gợi ý: `CreatedAt`, và `(AuditType, StatusCode, CreatedAt)`. Cần migration.

### Phân loại và error_message

- **Exception**: request bị throw exception. `StatusCode` là status mà `GlobalExceptionHandler` trả. `ErrorMessage` = `"{ExceptionType}: {Message}"`. Để middleware biết, `GlobalExceptionHandler` lưu exception vào `HttpContext.Items`, không dựa vào `IExceptionHandlerFeature`.
- **Activity**: còn lại. Nếu status >= 400 mà không có exception (ví dụ `Result.Failure`, lỗi validate) thì `ErrorMessage` lấy từ response JSON theo thứ tự `message`, `detail`, `title`; không đọc được thì để null.

### Pipeline

1. Middleware đặt **ngoài** `UseExceptionHandler` để thấy được response cuối cùng.
2. Đọc request body (bật buffering, tua lại), thay `Response.Body` bằng stream tạm để bắt response, gọi `next`, chép response ngược lại cho client.
3. Sau `next` mới đọc `HttpContext.User` (authentication chạy sau middleware nhưng user nằm trên cùng `HttpContext`).
4. Dựng bản ghi (mã hóa body và changes), gọi `TryWrite` vào channel rồi trả về. Mọi lỗi trong middleware audit chỉ được log.

### Channel và worker

- Channel có giới hạn (bounded), dung lượng cấu hình được (mặc định 10.000), một reader. Khi đầy thì bỏ bản ghi mới và log cảnh báo, không chặn request.
- `BackgroundService` đọc batch từ channel, ghi bằng `DbContext` của scope riêng (`IServiceScopeFactory`). Lỗi ghi thì log và thử lại vài lần rồi bỏ. Khi tắt app thì cố gắng drain phần còn lại.
- Publisher là abstraction trong Application (ví dụ `IAuditLogPublisher`), cài đặt channel nằm ở Infrastructure.

### Mã hóa

- AES-256-GCM, nonce ngẫu nhiên mỗi lần, lưu `v1:` + base64(nonce | tag | ciphertext). Tiền tố `v1:` chừa chỗ cho xoay khóa sau.
- Khóa 32 byte base64 lấy từ section cấu hình riêng (ví dụ `AuditEncryption:Key`). `appsettings.json` chỉ chứa placeholder cho dev; môi trường thật nạp qua biến môi trường/secret.
- Abstraction `IAuditEncryptor` trong Application, cài đặt ở Infrastructure.

### Thay đổi entity

- Scoped service (ví dụ `IAuditContext`) gom thay đổi trong một request.
- `ApplicationDbContext.SaveChangesAsync`: trước khi lưu, chụp các entry Added/Modified/Deleted (bỏ qua entity audit để tránh đệ quy); sau khi lưu thành công mới đẩy vào `IAuditContext`. Mỗi thay đổi gồm tên entity, id, hành động; Added lưu giá trị mới, Modified chỉ lưu các thuộc tính đổi (cũ → mới), Deleted lưu giá trị cũ.
- Middleware lấy danh sách này, serialize JSON, mã hóa, gán vào `Changes`. Ngoài HTTP request thì bỏ qua.
- Lưu hết thuộc tính (kể cả nhạy cảm như password hash) vì đã mã hóa, đúng yêu cầu "lưu hết".

### API đọc

- `GET /api/audit-logs?page=&pageSize=&status=&auditType=`: sắp xếp `CreatedAt` giảm dần; `pageSize` mặc định 20, tối đa 100; trả `items`, `page`, `pageSize`, `totalCount`. `items` chỉ có metadata, **không** có body/changes.
- `GET /api/audit-logs/{id}`: trả đầy đủ cột, trong đó `RequestBody`, `ResponseBody`, `Changes` vẫn là chuỗi mã hóa (API giải mã làm sau). 404 nếu không có.
- Làm theo mẫu hiện có: MediatR query trong `Application/Features/AuditLogs/Queries`, kết quả bọc `Result<T>`, controller kế thừa `BaseController`.

## Những lựa chọn tôi tự quyết, cần người dùng soát

- Giới hạn kích thước body lưu: mặc định 1 MB, cấu hình được. Body vượt trần bị cắt trước khi mã hóa, và hai cột bool `IsRequestTruncated`, `IsResponseTruncated` đánh dấu việc này (không nhét marker vào nội dung để API giải mã sau này trả đúng dữ liệu). Người dùng nói "lưu hết"; tôi thêm trần để tránh tràn bộ nhớ và dòng quá lớn.
- Body không phải văn bản (multipart/file upload, file download, binary, stream): lưu chuỗi giữ chỗ `[binary content, N bytes]` thay vì nội dung, vì không đọc lại được và rất nặng. Kiểu văn bản gồm JSON, XML, `text/*`, `application/x-www-form-urlencoded`.
- Bộ lọc `status` là mã HTTP chính xác (ví dụ `status=404`). Lọc "mọi lỗi" dùng `AuditType=Exception`; lọc theo nhóm 4xx/5xx để mở rộng sau nếu cần.
- Thiếu hoặc sai khóa mã hóa lúc khởi động thì app **không khởi động** (kiểm tra options khi start). Đây là ngoại lệ có chủ ý so với nguyên tắc "audit không làm lỗi request": thà không chạy còn hơn lưu plaintext hoặc mất log âm thầm.
- Hai API đọc chưa yêu cầu đăng nhập/role, giống các controller hiện có (dự án đang ở mức setup). Đây là khoảng trống đã biết: API lộ `Username`, `UserId`, `Path`, `ErrorMessage` và chuỗi mã hóa; cần thêm `[Authorize]`/policy trước khi dùng thật.
- Chưa có chính sách giữ/xóa dữ liệu cũ (retention, purge, archive). Bảng sẽ lớn dần; đây là khoảng trống đã biết, cần làm ở bước sau.
- `ErrorMessage` không kèm stack trace.
- Channel đầy thì bỏ bản ghi chứ không chờ.

## Để builder quyết

Tên lớp/file cụ thể, vị trí đặt từng interface, kiểu serialize JSON của `Changes`, cách batch, số lần retry, nội dung migration, và test (chưa có project test nào).
