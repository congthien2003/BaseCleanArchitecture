# JWT Authentication + User Entity — Design Spec

**Date:** 2026-06-12  
**Status:** Approved  
**Scope:** Full Register/Login flow with JWT, User/Role entities, Result pattern

## 1. Objective

Triển khai đầy đủ service `JwtTokenService` + `User` entity cơ bản + Register/Login endpoint. Kết hợp Role-based authorization, Result pattern immutable, theo Clean Architecture.

## 2. Current State

| Đã có | Thiếu |
|---|---|
| `JwtOptions` cơ bản | `IJwtTokenService` là **class** rỗng → cần sửa thành interface |
| `CurrentUser` model | `User` entity trong Domain |
| `CurrentUserService` (hardcoded) | Implementation `JwtTokenService` |
| `Claims` class rỗng | Package JWT (`JwtBearer`) |
| `UseAuthorization()` in Program.cs | `UseAuthentication()` middleware |
| `Result` pattern (mutable) | Cần sửa → immutable record |

## 3. Domain Layer

**Entities:**

| Entity | Base | Key Fields |
|---|---|---|
| `User` | `EntityAuditBase<Guid>` | Username(50), Email(100), PasswordHash, Salt, FullName(100), PhoneNumber(20), IsEmailConfirmed, IsPhoneNumberConfirmed, IsTwoFactorEnabled, LastLoginAt, RefreshToken(500), RefreshTokenExpiry |
| `Role` | `EntityAuditBase<Guid>` | Name(50) unique, Description(200) |
| `UserRole` | `EntityAuditBase<Guid>` | UserId, RoleId — composite unique index |

**Domain Events:** `UserCreatedEvent`, `UserLoggedInEvent`

**Seed Roles:** `Admin` (id: `11111111-1111-1111-1111-111111111111`), `Customer` (id: `22222222-2222-2222-2222-222222222222`)

## 4. Application Layer

**Interfaces (sửa/mới):**

| Interface | Vị trí | Mô tả |
|---|---|---|
| `IJwtTokenService` | Sửa từ class → interface | `GenerateToken(User) → AuthTokenDto`, `ValidateToken(string) → ClaimsPrincipal?`, `GenerateRefreshToken() → string` |
| `IAuthService` | Mới | `RegisterAsync(RegisterDto) → User`, `LoginAsync(string username, string password) → User` |
| `IPasswordHasher` | Mới | `Hash(string) → (hash, salt)`, `Verify(string password, string hash, string salt) → bool` |

**CQRS Commands:**

| Command | Request | Response |
|---|---|---|
| `RegisterCommand` | Username, Email, Password, FullName, PhoneNumber? | `Result<AuthTokenDto>` |
| `LoginCommand` | Username, Password | `Result<AuthTokenDto>` |

**DTOs (API layer):**

| DTO | Fields |
|---|---|
| `RegisterRequest` (API DTO) | Username, Email, Password, FullName, PhoneNumber? |
| `LoginRequest` (API DTO) | Username, Password |

**Application DTOs/Models:**

| Model | Fields |
|---|---|
| `AuthTokenDto` | AccessToken, RefreshToken, ExpiresAt |
| `RegisterDto` | Username, Email, Password, FullName, PhoneNumber? |

**Models sửa:**

- `Result<T>` → immutable `sealed record`, factory methods `Success`/`Failure`
- `CurrentUser` → thêm `IsAuthenticated`, `IsAdmin()`, `IsCustomer()`, `IsInRole(string)`

## 5. Infrastructure Layer

**Package:** `Microsoft.AspNetCore.Authentication.JwtBearer` (vào Infrastructure.csproj)

**Implementations:**

| Class | Interface | Notes |
|---|---|---|
| `JwtTokenService` | `IJwtTokenService` | Dùng `JwtSecurityTokenHandler` + `SymmetricSecurityKey` |
| `PasswordHasher` | `IPasswordHasher` | HMACSHA256 + salt per user |

**Options (sửa):**

`JwtOptions` → thêm `const string SectionName`, chuyển `set` → `init`

**DI:** `AddAuthServices(IConfiguration)` extension method trong `Infrastructure/Auth/AuthDependencyInjection.cs`

**Claims trong token:** `sub`(UserId), `unique_name`(Username), `email`(Email), `role`(multiple — từ UserRoles)

## 6. Persistence Layer

**Configurations:** `UserConfiguration`, `RoleConfiguration`, `UserRoleConfiguration`

**DbContext:** Thêm `DbSet<User>`, `DbSet<Role>`, `DbSet<UserRole>`

**CurrentUserService:** Sửa để extract từ `IHttpContextAccessor.HttpContext.User.Claims`

**DI:** Thêm `AddHttpContextAccessor()`

## 7. WebAPI Layer

**Controller:** `AuthController` với `POST /api/auth/register`, `POST /api/auth/login` — explicit `{ }` block, gọi `_mediator.Send(command)`, trả `Ok(result)`.

**Program.cs:** Thêm `builder.Services.AddJwtAuthentication(config)` + `app.UseAuthentication()` trước `app.UseAuthorization()`.

**appsettings.json:** Thêm section `"Jwt"` với `Issuer`, `Audience`, `SecretKey`, `ExpiryMinutes`.

## 8. Verification Plan

1. Build solution không lỗi
2. Gọi `POST /api/auth/register` → nhận token hợp lệ, decode chứa đúng claims
3. Gọi `POST /api/auth/login` → nhận token, user có LastLoginAt cập nhật
4. Gọi endpoint có `[Authorize]` với token → thành công
5. Gọi với token hết hạn/invalid → 401
6. `CurrentUser.IsAdmin()` / `IsCustomer()` hoạt động đúng theo role

## 9. Risks / Unknowns

- PostgreSQL chưa chạy → cần đảm bảo migration hoặc dùng `EnsureCreated` cho dev
- SecretKey trong appsettings → production phải dùng User Secrets / env
- Chưa có refresh token endpoint → scope future
