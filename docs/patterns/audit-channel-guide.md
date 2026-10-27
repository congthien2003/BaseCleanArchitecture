# Audit log theo pattern Channel (Publisher → Worker)

Tài liệu mô tả cách phần audit log của project được dựng: middleware ghi nhận request, đẩy vào `Channel<AuditLog>`, một worker nền đọc từ channel rồi ghi xuống PostgreSQL (schema `audit`). Request/response/changes được mã hóa AES-256-GCM trước khi lưu.

Code mẫu trong tài liệu đã được rút gọn để dễ hình dung (bỏ log, null-check, một số chi tiết phụ). Muốn xem bản đầy đủ, mở file được ghi ở đầu mỗi mục.

---

## 1. Tổng quan

### Vì sao dùng Channel

Ghi audit thẳng xuống DB trong request sẽ làm chậm mọi API và khiến lỗi DB audit kéo sập request. Pattern producer/consumer tách hai việc:

| Vai trò | Thành phần | Việc làm |
|---|---|---|
| Publisher (producer) | `AuditLoggingMiddleware` | Dựng `AuditLog`, mã hóa, `TryWrite` vào channel rồi trả response ngay |
| Hàng đợi | `AuditLogChannel` (`Channel<AuditLog>` bounded) | Giữ log chờ ghi, đầy thì bỏ log mới và cảnh báo |
| Consumer | `AuditLogWorker` (`IHostedService`) | Đọc channel theo batch, lưu DB, retry, drain khi tắt app |

Nguyên tắc: **audit không bao giờ làm hỏng request.** Mọi lỗi trong lúc audit chỉ được log lại rồi nuốt.

### Luồng dữ liệu

```
 Request
    │
    ▼
┌──────────────────────────────┐
│ AuditLoggingMiddleware       │  (đặt TRƯỚC UseExceptionHandler)
│  1. đọc request body         │
│  2. bọc Response.Body        │
│  3. await next(context) ─────┼──► Controller → MediatR → Handler
│                              │                              │
│                              │                  SaveChangesAsync (DbContext)
│                              │                              │
│                              │      IAuditContext.AddChanges(...)  ◄─┘
│  4. dựng AuditLog + mã hóa   │
│  5. publisher.TryPublish ────┼──► Channel<AuditLog> (bounded)
└──────────────────────────────┘                │
                                                ▼
                                   ┌──────────────────────────┐
                                   │ AuditLogWorker           │
                                   │  đọc batch ≤ BatchSize   │
                                   │  retry → fallback từng cái│
                                   └────────────┬─────────────┘
                                                ▼
                                    audit."AuditLogs" (PostgreSQL)
```

### Mỗi request = 1 dòng

Thay đổi entity (Added/Modified/Deleted) **gộp vào cùng dòng** của request đó, nằm ở cột `Changes`, không tách thành dòng riêng.

---

## 2. Vị trí các file

| Layer | File | Vai trò |
|---|---|---|
| Domain | `Entities/AuditLog.cs` | Entity (Guid v7) |
| Domain | `Enums/AuditType.cs` | `Activity`, `Exception` |
| Domain | `Abtractions/Repositories/IAuditLogRepository.cs` | Truy vấn list có phân trang + filter |
| Application | `Abstractions/Auditing/IAuditLogPublisher.cs` | Cổng publish cho middleware |
| Application | `Abstractions/Auditing/IAuditEncryptor.cs` | Mã hóa / giải mã |
| Application | `Abstractions/Auditing/IAuditContext.cs` | Gom entity changes trong 1 request |
| Application | `Features/AuditLogs/Queries/*` | `GetAuditLogsQuery`, `GetAuditLogByIdQuery` |
| Infrastructure | `Auditing/AuditLogChannel.cs` | Channel + publisher |
| Infrastructure | `Auditing/AuditLogWorker.cs` | Consumer |
| Infrastructure | `Auditing/AesGcmAuditEncryptor.cs` | AES-256-GCM |
| Infrastructure | `Auditing/AuditContext.cs` | Implement `IAuditContext` (scoped) |
| Infrastructure | `Auditing/AuditOptions.cs`, `AuditEncryptionOptions.cs` | Cấu hình |
| Infrastructure | `Auditing/AuditServiceCollectionExtensions.cs` | `AddAuditServices` |
| Persistence | `Auditing/EntityChangeSnapshot.cs` | Chụp thay đổi quanh `SaveChanges` |
| Persistence | `ApplicationDbContext.cs` | Gọi snapshot trong `SaveChangesAsync` |
| Persistence | `Configurations/AuditLogConfiguration.cs` | Mapping, schema `audit`, index |
| WebAPI | `Middleware/AuditLogging/*` | Middleware, `CapturingStream`, `AuditBodyCapture`, `AuditErrorMessage` |
| WebAPI | `Controllers/Features/AuditLogsController.cs` | 2 API đọc |

---

## 3. Từng phần trong pattern

### 3.1 Entity `AuditLog`

`Domain/Entities/AuditLog.cs`

```csharp
public class AuditLog : EntityBase<Guid>
{
    public AuditType AuditType { get; set; }      // Activity | Exception
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }     // plaintext, để lọc/hiển thị nhanh
    public string HttpMethod { get; set; }
    public string Path { get; set; }              // không kèm query string
    public Guid? UserId { get; set; }             // null nếu chưa đăng nhập
    public string? Username { get; set; }
    public long DurationMs { get; set; }

    public string? RequestBody { get; set; }     // MÃ HÓA
    public string? ResponseBody { get; set; }    // MÃ HÓA
    public string? Changes { get; set; }         // MÃ HÓA (JSON list entity changes)

    public bool IsRequestTruncated { get; set; }
    public bool IsResponseTruncated { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public AuditLog() : base(Guid.CreateVersion7()) { }
}
```

Ý chính:
- `Guid v7` sắp xếp được theo thời gian nên chèn nhanh và index đẹp.
- Chỉ ba cột `RequestBody`, `ResponseBody`, `Changes` được mã hóa. `ErrorMessage`, `Username`, `Path` để plaintext vì dùng để lọc/xem nhanh, vì vậy **không được để dữ liệu nhạy cảm lọt vào các cột này** (xem mục 6).
- Header **không lưu**, chỉ lưu body.

### 3.2 Publisher: `IAuditLogPublisher` + `AuditLogChannel`

`Infrastructure/Auditing/AuditLogChannel.cs`

```csharp
public sealed class AuditLogChannel : IAuditLogPublisher
{
    private readonly Channel<AuditLog> _channel;

    public AuditLogChannel(IOptions<AuditOptions> options, ILogger<AuditLogChannel> logger)
    {
        _channel = Channel.CreateBounded<AuditLog>(new BoundedChannelOptions(options.Value.ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait, // TryWrite trả false khi đầy
            SingleReader = true,                    // chỉ 1 worker đọc
        });
    }

    public ChannelReader<AuditLog> Reader => _channel.Reader;

    public bool TryPublish(AuditLog log)
    {
        if (_channel.Writer.TryWrite(log)) return true;
        _logger.LogWarning("Audit log dropped (channel full or closed)");
        return false;                                // không bao giờ chờ, không ném lỗi
    }

    public void Complete() => _channel.Writer.TryComplete();
}
```

Lý do chọn:
- **Bounded** để RAM không phình khi DB chậm. Đầy thì *bỏ log mới và cảnh báo*, chấp nhận mất audit còn hơn làm chậm API.
- Dùng `TryWrite` (không `await WriteAsync`) nên middleware không bao giờ bị block.
- `FullMode = Wait` kết hợp `TryWrite`: khi đầy chỉ trả `false`, không âm thầm vứt log đã nằm trong hàng đợi (khác `DropOldest`).
- Đăng ký **singleton**, cùng một instance vừa là `IAuditLogPublisher` (cho middleware) vừa được inject nguyên kiểu `AuditLogChannel` vào worker để lấy `Reader`.

### 3.3 Consumer: `AuditLogWorker`

`Infrastructure/Auditing/AuditLogWorker.cs`

```csharp
public sealed class AuditLogWorker : IHostedService
{
    public Task StartAsync(CancellationToken ct)
    {
        _loop = Task.Run(RunAsync, CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _channel.Complete();               // không nhận thêm
        await _loop.WaitAsync(ct);         // đợi worker ghi hết phần còn lại (drain)
    }

    private async Task RunAsync()
    {
        var reader = _channel.Reader;
        while (await reader.WaitToReadAsync(CancellationToken.None))
        {
            var batch = new List<AuditLog>(batchSize);
            while (batch.Count < batchSize && reader.TryRead(out var log))
                batch.Add(log);

            if (batch.Count > 0) await PersistAsync(batch);
        }
    }
}
```

Lưu một batch:

```csharp
private async Task PersistAsync(List<AuditLog> batch)
{
    if (await TrySaveWithRetryAsync(batch)) return;          // 1) cả batch, có retry

    if (batch.Count == 1) { /* log lỗi, bỏ */ return; }

    foreach (var log in batch)                                // 2) tách ra lưu từng cái
        if (!await TrySaveWithRetryAsync([log])) { /* log lỗi, bỏ riêng cái này */ }
}

private async Task<bool> TrySaveWithRetryAsync(List<AuditLog> batch)
{
    for (var attempt = 1; attempt <= 1 + WriteRetries; attempt++)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();   // scope mới mỗi lần
            var repo = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
            var uow  = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            foreach (var log in batch) await repo.AddAsync(log);
            await uow.SaveChangesAsync();
            return true;
        }
        catch { await Task.Delay(RetryDelayMs * attempt); }             // backoff tuyến tính
    }
    return false;
}
```

Lý do chọn:
- **Không dùng `BackgroundService`.** Trên .NET 10, nếu `StopAsync` đến ngay sau `StartAsync` thì `ExecuteAsync` bị hủy trước khi chạy, log đang chờ sẽ mất. Dùng `IHostedService` với vòng lặp riêng chỉ kết thúc khi channel đã complete và đã drain.
- **Worker là singleton, DbContext là scoped** nên mỗi lần lưu phải tạo scope mới qua `IServiceScopeFactory`.
- **Batch + fallback từng cái**: một bản ghi "độc" (ví dụ chứa ký tự mà Postgres từ chối) chỉ mất chính nó, không kéo theo cả batch.
- Khi app tắt: `Complete()` rồi chờ drain, nên log còn trong hàng đợi vẫn được ghi.

### 3.4 Middleware: `AuditLoggingMiddleware`

`WebAPI/Middleware/AuditLogging/AuditLoggingMiddleware.cs`

Khung xử lý (đã rút gọn):

```csharp
public async Task InvokeAsync(HttpContext context, IAuditLogPublisher publisher,
    IAuditEncryptor encryptor, IAuditContext auditContext,
    ICurrentUserService currentUser, IOptions<AuditOptions> options)
{
    var stopwatch = Stopwatch.StartNew();

    var request = await CaptureRequestAsync(context.Request, maxBytes);   // (a)

    var originalBody = context.Response.Body;
    var response = new CapturingStream(originalBody, maxBytes);            // (b)
    context.Response.Body = response;

    Exception? escaped = null;
    try   { await _next(context); }
    catch (Exception ex) { escaped = ex; throw; }
    finally
    {
        context.Response.Body = originalBody;
        stopwatch.Stop();
        try
        {
            var log = BuildLog(...);                                       // (c)
            publisher.TryPublish(log);                                     // (d)
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to build audit log"); }
    }
}
```

**(a) Đọc request body**
- Chỉ đọc body dạng text (JSON, XML, form...). Body nhị phân (upload file) **không đọc**, chỉ ghi lại kích thước khai báo.
- `EnableBuffering()` để controller vẫn đọc lại được body sau khi middleware đã đọc, rồi `Position = 0`.
- Chỉ giữ tối đa `MaxBodyBytes` đầu tiên, phần còn lại chỉ đếm để biết có bị cắt (`IsRequestTruncated`).

**(b) Ghi nhận response bằng `CapturingStream`** (`CapturingStream.cs`)

```csharp
internal sealed class CapturingStream : Stream
{
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        await _inner.WriteAsync(buffer, ct);   // gửi cho client NGAY
        Capture(buffer.Span);                  // chỉ giữ bản sao của MaxBytes đầu
    }

    private void Capture(ReadOnlySpan<byte> buffer)
    {
        TotalBytes += buffer.Length;
        var room = _maxBytes - (int)_captured.Length;
        if (room > 0) _captured.Write(buffer[..Math.Min(room, buffer.Length)]);
    }
}
```

Dạng *write-through*: dữ liệu đi thẳng ra client, không giữ cả response trong RAM.

**(c) Dựng `AuditLog`** (rút gọn)

```csharp
var handled   = context.Items.TryGetValue(AuditHttpContextKeys.Exception, out var item) ? item as Exception : null;
var exception = handled ?? escaped;
var status    = escaped is not null && handled is null ? 500 : context.Response.StatusCode;

var errorMessage = exception is not null
    ? AuditErrorMessage.FromException(exception)                    // "TypeName: message"
    : status >= 400 ? AuditErrorMessage.FromResponse(responseText, contentType) : null;

return new AuditLog
{
    AuditType    = exception is not null ? AuditType.Exception : AuditType.Activity,
    StatusCode   = status,
    ErrorMessage = errorMessage,
    UserId       = user.Id == Guid.Empty ? null : user.Id,
    Username     = string.IsNullOrEmpty(user.Username) ? null : user.Username,
    RequestBody  = requestText  is null ? null : encryptor.Encrypt(requestText),
    ResponseBody = responseText is null ? null : encryptor.Encrypt(responseText),
    Changes      = changes.Count == 0 ? null : encryptor.Encrypt(JsonSerializer.Serialize(changes)),
    // ...HttpMethod, Path, DurationMs, IsRequestTruncated, IsResponseTruncated, CreatedAt
};
```

Quy tắc đặt giá trị:

| Trường | Quy tắc |
|---|---|
| `AuditType` | `Exception` nếu có exception (đã bắt hay lọt ra), ngược lại `Activity` |
| `StatusCode` | Exception lọt khỏi pipeline mà chưa được handler xử lý → 500; còn lại lấy từ response |
| `ErrorMessage` | Exception → `"{TênKiểu}: {Message}"`. Không exception mà status ≥ 400 → lấy `message`, rồi `detail`, rồi `title` trong JSON response. Tối đa 4000 ký tự, bỏ ký tự `\0` |
| `UserId`/`Username` | Đọc **sau** `next` (lúc đó authentication đã chạy); null nếu chưa đăng nhập |
| Body không phải text | Thay bằng `[binary content, N bytes]` |
| `Path` | `PathBase + Path`, cắt 2048, **không có query string** |

**(d) Bắt lỗi từ `Result<T>`**: project trả 4xx qua `Result<T>` mà không ném exception, nên các lỗi kiểu "sai mật khẩu" (400) sẽ có `AuditType = Activity` nhưng `ErrorMessage` lấy từ JSON response. Chỉ khi thật sự có exception mới là `Exception`.

**Thứ tự middleware trong `Program.cs`** (quan trọng):

```csharp
app.UseMiddleware<AuditLoggingMiddleware>();   // ngoài cùng, thấy response cuối cùng
app.UseExceptionHandler();                      // GlobalExceptionHandler
// ... authentication, authorization, controllers
```

Đặt audit **trước** `UseExceptionHandler` để nó thấy response đã được handler chuyển thành 500/ProblemDetails.

### 3.5 Móc exception đã được xử lý: `GlobalExceptionHandler`

Khi `GlobalExceptionHandler` bắt exception và ghi response, exception không còn "lọt" lên middleware nữa. Handler vì thế cất nó vào `HttpContext.Items` để middleware lấy:

```csharp
// GlobalExceptionHandler.TryHandleAsync
httpContext.Items[AuditHttpContextKeys.Exception] = exception;
```

Middleware đọc lại ở bước (c) bằng `context.Items.TryGetValue(AuditHttpContextKeys.Exception, ...)`. Nhờ vậy dòng audit mang đúng loại và message của exception gốc.

### 3.6 Mã hóa: `IAuditEncryptor` + `AesGcmAuditEncryptor`

```csharp
public interface IAuditEncryptor
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
```

`Infrastructure/Auditing/AesGcmAuditEncryptor.cs`:

```csharp
public string Encrypt(string plaintext)
{
    var plain   = Encoding.UTF8.GetBytes(plaintext);
    var payload = new byte[NonceSize + TagSize + plain.Length];   // 12 + 16 + n
    var nonce = payload.AsSpan(0, 12);
    var tag   = payload.AsSpan(12, 16);
    var cipher = payload.AsSpan(28);

    RandomNumberGenerator.Fill(nonce);                            // nonce mới mỗi lần
    using var aes = new AesGcm(_key, tagSizeInBytes: 16);
    aes.Encrypt(nonce, plain, cipher, tag);

    return "v1:" + Convert.ToBase64String(payload);
}
```

- Định dạng lưu: `v1:` + base64( `nonce(12) | tag(16) | ciphertext` ).
- Tiền tố `v1:` chừa chỗ cho việc xoay khóa sau này (thêm `v2:` mà vẫn giải mã được dữ liệu cũ).
- GCM có xác thực (tag) nên dữ liệu bị sửa trong DB sẽ làm `Decrypt` ném `CryptographicException`.
- Khóa nằm ở `AuditEncryption:Key` = base64 của đúng **32 byte**. Cấu hình `ValidateOnStart()` nên **app không khởi động** nếu thiếu/sai khóa (thà không chạy còn hơn ghi plaintext hay mất log âm thầm).

Tạo khóa:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

### 3.7 Thay đổi entity: `IAuditContext` + `EntityChangeSnapshot`

Ý tưởng: lúc `SaveChangesAsync` ta biết chính xác entity nào đổi gì, nhưng middleware thì không. Hai bên giao tiếp qua `IAuditContext` (scoped theo request).

```csharp
// ApplicationDbContext.SaveChangesAsync (rút gọn)
var entityChanges = EntityChangeSnapshot.Capture(ChangeTracker);   // TRƯỚC khi save

var result = await base.SaveChangesAsync(cancellationToken);

if (result > 0)                                                    // chỉ ghi khi save thành công
    _auditContext.AddChanges(entityChanges.Resolve());             // SAU khi save (đã có Id sinh tự động)
```

`Persistence/Auditing/EntityChangeSnapshot.cs`:

```csharp
foreach (var entry in changeTracker.Entries())
{
    if (entry.Entity is AuditLog) continue;        // không audit chính bảng audit (tránh đệ quy)

    var (action, props) = entry.State switch
    {
        EntityState.Added    => ("Added",    props => new PropertyChange(null, current)),
        EntityState.Deleted  => ("Deleted",  props => new PropertyChange(original, null)),
        EntityState.Modified => ("Modified", /* chỉ thuộc tính thực sự đổi */),
        _ => (null, null),
    };
    // lưu (entry, action, props)
}
```

Kết quả (sau khi serialize + mã hóa vào cột `Changes`):

```json
[
  {
    "Entity": "Category", "Id": "0198...", "Action": "Modified",
    "Properties": { "Name": { "Old": "Cũ", "New": "Mới" } }
  }
]
```

Lý do thiết kế:
- Phải **chụp trước khi save** vì sau khi save, `EntityState` và giá trị gốc đã bị reset.
- **Resolve sau khi save** để lấy được khóa chính do DB/EF sinh ra.
- Giới hạn `MaxChangesPerRequest` (mặc định 1000). Quá số đó, phần dư được đếm và thêm một mục cuối `{ "Entity": "*", "Action": "Truncated", "Properties": { "Dropped": { "New": N } } }` để import hàng loạt không làm phình RAM.
- `AuditContext` dùng `lock` vì một request có thể gọi `SaveChanges` từ nhiều luồng.

### 3.8 Lưu trữ: schema `audit`

`Persistence/Configurations/AuditLogConfiguration.cs`:

```csharp
builder.ToTable("AuditLogs", "audit");
builder.Property(a => a.AuditType).HasConversion<string>().HasMaxLength(20);
builder.Property(a => a.RequestBody).HasColumnType("text");
builder.Property(a => a.ResponseBody).HasColumnType("text");
builder.Property(a => a.Changes).HasColumnType("text");

builder.HasIndex(a => a.CreatedAt);
builder.HasIndex(a => new { a.AuditType, a.StatusCode, a.CreatedAt });   // phục vụ filter + sắp xếp
```

- Migration `AddAuditLogs` chỉ đụng schema `audit`.
- `AuditType` lưu dạng chuỗi (`"Activity"`/`"Exception"`) cho dễ đọc khi query trực tiếp.

### 3.9 Hai API đọc

`WebAPI/Controllers/Features/AuditLogsController.cs`

```csharp
[Route("api/audit-logs")]
public class AuditLogsController : BaseController
{
    [HttpGet]                  // list: phân trang + filter
    public Task<IActionResult> GetListAsync(int page = 1, int pageSize = 20,
        int? status = null, AuditType? auditType = null, CancellationToken ct = default);

    [HttpGet("{id:guid}")]     // chi tiết
    public Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct = default);
}
```

Ví dụ:

```
GET /api/audit-logs?page=1&pageSize=20
GET /api/audit-logs?status=400
GET /api/audit-logs?auditType=Exception&status=500
GET /api/audit-logs/0198a3c0-....
```

- `status` là **mã HTTP chính xác** (400, 500...), không phải nhóm 4xx/5xx.
- `auditType` sai giá trị → 400 (model binding của enum).
- Chuẩn hóa phân trang: `page < 1` → 1, `pageSize <= 0` → 20, `pageSize > 100` → 100.
- Sắp xếp `CreatedAt` giảm dần.
- ID không tồn tại → 404.
- **Chi tiết hiện trả chuỗi đã mã hóa** của `RequestBody`/`ResponseBody`/`Changes`. API trả bản giải mã sẽ làm riêng sau (xem mục 5).

---

## 4. Cấu hình & đăng ký

### 4.1 `appsettings.json`

```json
"Audit": {
  "ChannelCapacity": 10000,
  "MaxBodyBytes": 1048576,
  "BatchSize": 100,
  "WriteRetries": 3,
  "RetryDelayMs": 200
},
"AuditEncryption": {
  "Key": ""
}
```

| Khóa | Mặc định | Ý nghĩa |
|---|---|---|
| `ChannelCapacity` | 10000 | Sức chứa hàng đợi; đầy thì bỏ log mới |
| `MaxBodyBytes` | 1 MiB | Giữ tối đa bao nhiêu byte mỗi body; quá thì cắt và bật cờ `Is*Truncated` |
| `MaxChangesPerRequest` | 1000 | Số entity change tối đa mỗi request (không có sẵn trong `appsettings.json`, đang dùng mặc định; thêm vào section `Audit` nếu muốn đổi) |
| `BatchSize` | 100 | Số log ghi mỗi lần |
| `WriteRetries` | 3 | Số lần **thử thêm** sau lần đầu thất bại (tổng 1 + 3 lần) |
| `RetryDelayMs` | 200 | Chờ `RetryDelayMs × số lần thử` giữa các lần |
| `AuditEncryption:Key` | rỗng | Base64 của 32 byte. **Bắt buộc**, để trống thì app không khởi động |

`appsettings.Development.json` có sẵn một khóa dev. Môi trường thật phải đưa khóa qua biến môi trường hoặc secret store, không commit:

```
AuditEncryption__Key=<base64-32-byte>
```

### 4.2 Đăng ký DI

`Infrastructure/DependencyInjection.cs` gọi `AddAuditServices(configuration)`:

```csharp
services.AddOptions<AuditOptions>().Bind(configuration.GetSection("Audit"));

services.AddOptions<AuditEncryptionOptions>()
    .Bind(configuration.GetSection("AuditEncryption"))
    .Validate(o => AuditEncryptionOptions.IsValidKey(o.Key), "AuditEncryption:Key must be base64 of 32 bytes.")
    .ValidateOnStart();

services.AddSingleton<IAuditEncryptor, AesGcmAuditEncryptor>();
services.AddScoped<IAuditContext>(sp => new AuditContext(sp.GetRequiredService<IOptions<AuditOptions>>().Value.MaxChangesPerRequest));

services.AddSingleton<AuditLogChannel>();
services.AddSingleton<IAuditLogPublisher>(sp => sp.GetRequiredService<AuditLogChannel>());   // cùng 1 instance
services.AddHostedService<AuditLogWorker>();
```

Điểm cần nhớ về lifetime:

| Thành phần | Lifetime | Vì sao |
|---|---|---|
| `AuditLogChannel`, `IAuditLogPublisher` | Singleton | Một hàng đợi duy nhất toàn app |
| `AuditLogWorker` | Hosted service (singleton) | Một consumer |
| `IAuditEncryptor` | Singleton | Giữ khóa, không có state theo request |
| `IAuditContext` | **Scoped** | Mỗi request một danh sách changes riêng |
| `IAuditLogRepository`, `IUnitOfWork` | Scoped | Worker tự tạo scope mỗi lần lưu |

---

## 5. Cách triển khai lại / mở rộng

### Dựng lại từ đầu trong project khác

1. **Domain**: `AuditLog`, `AuditType`, `IAuditLogRepository`.
2. **Application**: `IAuditLogPublisher`, `IAuditEncryptor`, `IAuditContext` + record `EntityChange`/`PropertyChange`.
3. **Infrastructure**: `AuditLogChannel`, `AuditLogWorker`, `AesGcmAuditEncryptor`, `AuditContext`, options + `AddAuditServices`.
4. **Persistence**: cấu hình EF (schema `audit`), repository, `EntityChangeSnapshot`, thêm `IAuditContext` vào `ApplicationDbContext` và gọi snapshot trong `SaveChangesAsync`, rồi tạo migration.
5. **WebAPI**: middleware + `CapturingStream` + helper, gắn `Items[...]` trong exception handler, đăng ký middleware **trước** `UseExceptionHandler`.
6. Đặt khóa mã hóa và chạy migration.

### Thêm trường audit mới (ví dụ `TraceId`, `ClientIp`)

1. Thêm property vào `AuditLog` và cấu hình trong `AuditLogConfiguration`.
2. Tạo migration.
3. Gán trong `BuildLog` của middleware.
4. Thêm vào `AuditLogListItemDto`/`AuditLogDetailDto` nếu muốn trả ra API.

### Loại trừ endpoint khỏi audit

Hiện tại log hết mọi request (đã chủ động chọn vì dữ liệu được mã hóa). Nếu cần bỏ qua health check hay Swagger, thêm điều kiện ở đầu `InvokeAsync`:

```csharp
if (context.Request.Path.StartsWithSegments("/health"))
{
    await _next(context);
    return;
}
```

### Làm API trả dữ liệu đã giải mã (việc sẽ làm sau)

Trong handler của `GetAuditLogByIdQuery`, inject `IAuditEncryptor` và giải mã trước khi trả:

```csharp
var dto = new AuditLogDecryptedDto
{
    // ...các trường plaintext
    RequestBody  = log.RequestBody  is null ? null : encryptor.Decrypt(log.RequestBody),
    ResponseBody = log.ResponseBody is null ? null : encryptor.Decrypt(log.ResponseBody),
    Changes      = log.Changes      is null ? null : encryptor.Decrypt(log.Changes),
};
```

Nhớ đặt `[Authorize]` và policy riêng cho API này (dữ liệu giải mã chứa nội dung request thật, gồm cả mật khẩu người dùng nhập nếu có).

### Xoay khóa mã hóa

Chưa làm, nhưng định dạng đã chừa chỗ: giữ nhiều khóa theo phiên bản, `Encrypt` luôn dùng phiên bản mới nhất (`v2:`...), `Decrypt` đọc tiền tố để chọn khóa tương ứng.

---

## 6. Lưu ý & giới hạn đã biết

**Bảo mật**
- Hai API đọc hiện **chưa yêu cầu đăng nhập**: ai gọi cũng thấy `Username`, `UserId`, `Path`, `ErrorMessage` và chuỗi mã hóa. Cần thêm `[Authorize]`/policy trước khi dùng ngoài môi trường dev.
- `ErrorMessage`, `Username`, `Path` là plaintext. Message exception hoặc giá trị người dùng nhập có thể bị phản chiếu vào đây.
- `Path` không lưu query string vì query hay chứa token.
- Khóa dev nằm trong `appsettings.Development.json`; môi trường thật phải cấp khóa từ secret store.
- Body của request (ví dụ đăng nhập, đăng ký) được lưu nguyên trước khi mã hóa, nên mật khẩu dạng plaintext nằm trong dữ liệu sau khi giải mã. Cân nhắc che các trường nhạy cảm nếu sau này muốn.

**Độ tin cậy**
- Channel đầy hoặc app crash đột ngột thì mất log chưa kịp ghi (đánh đổi để không làm chậm API). Tắt app bình thường thì được drain.
- Nếu DB commit thành công nhưng mất xác nhận rồi retry, có thể dính lỗi trùng khóa chính cho batch đó.
- Thay đổi entity vẫn được ghi nhận nếu transaction ngoài rollback sau `SaveChanges`.
- Client ngắt kết nối giữa chừng có thể bị ghi là `Exception` 500.

**Hiệu năng / dung lượng**
- Chưa có chính sách xóa log cũ; bảng sẽ lớn dần. Nên có job purge theo `CreatedAt`.
- Body vượt `MaxBodyBytes` bị cắt, cờ `IsRequestTruncated`/`IsResponseTruncated` bật.
- Request không rõ độ dài (chunked) mà lớn hơn `MaxBodyBytes` vẫn phải đọc hết để đếm tổng kích thước.
- Thay `Response.Body` bằng `CapturingStream` làm mất `IHttpResponseBodyFeature`, và nếu có nén response thì log sẽ thấy bytes đã nén tùy vị trí middleware nén.

**API**
- `(page - 1) * pageSize` với `page` cực lớn có thể tràn int gây 500.
- Thứ tự list chưa ổn định khi nhiều dòng trùng `CreatedAt` (nên thêm `ThenByDescending(Id)`).

---

## 7. Kiểm thử

Test nằm ở `tests/BaseCleanArchitecture.Tests` (xunit, EF InMemory, `AspNetCore.TestHost`).

```bash
dotnet test
```

| Nhóm | Kiểm tra gì |
|---|---|
| Encryptor | Round-trip, nonce khác nhau mỗi lần, tag/phiên bản sai thì ném lỗi |
| Channel | Publish/đọc, đầy thì trả `false`, complete |
| Worker | Ghi batch, retry, bản ghi độc không kéo theo batch, drain khi stop |
| Middleware | Status, `ErrorMessage`, body mã hóa, binary placeholder, cắt body, không làm hỏng request khi audit lỗi |
| Entity changes | Added/Modified/Deleted, bỏ qua `AuditLog`, trần `MaxChangesPerRequest` |
| DI | App từ chối khởi động khi thiếu/sai khóa |
| Queries | Phân trang, filter `status`/`auditType`, 404 |

`TestSupport/AuditTestServer` dựng một host nhỏ có middleware thật để test end-to-end mà không cần Postgres.

---

## 8. Checklist khi vận hành

- [ ] Đã đặt `AuditEncryption:Key` (32 byte base64) cho môi trường, lấy từ secret store.
- [ ] Đã chạy migration `AddAuditLogs`.
- [ ] Đã thêm `[Authorize]`/policy cho `AuditLogsController`.
- [ ] Đã có kế hoạch xóa/lưu trữ log cũ.
- [ ] Đã theo dõi cảnh báo `Audit log dropped` (channel đầy) và `Dropping audit log` (ghi DB thất bại).
