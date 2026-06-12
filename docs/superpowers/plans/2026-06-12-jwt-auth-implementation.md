# JWT Authentication + User Entity — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement full JWT auth flow with User/Role entities, Register/Login endpoints, Result pattern, and CurrentUser claims extraction — all following Clean Architecture.

**Architecture:** 6-task decomposition following layer dependency order. Domain first, then Application interfaces, then Infrastructure + CQRS + Persistence in parallel, then WebAPI integration last.

**Tech Stack:** .NET 10, MediatR 14.1.0, EF Core 10 + Npgsql (PostgreSQL), JwtBearer, Clean Architecture

**Key conventions from codebase:**
- Entities extend `EntityAuditBase<Guid>` (chain: `EntityBase<TKey>` → `IAuditable`)
- `ApplicationDbContext.SaveChangesAsync` auto-populates `CreatedAt/By`, `UpdatedAt/By` via `ICurrentUserService`
- Domain events cleared + dispatched **after** `SaveChangesAsync` succeeds
- Controllers use explicit `{ }` blocks, call `_mediator.Send(command)`, return `Ok(result)`
- DI: static extension methods (`AddXxx`) returning `IServiceCollection`
- Options: `const string SectionName`, `init` properties
- All async methods accept `CancellationToken`

---

### Task 1: Domain Entities + Events

**Files:**
- Create: `src/BaseCleanArchitecture.Domain/Entities/User.cs`
- Create: `src/BaseCleanArchitecture.Domain/Entities/Role.cs`
- Create: `src/BaseCleanArchitecture.Domain/Entities/UserRole.cs`
- Create: `src/BaseCleanArchitecture.Domain/Events/User/UserCreatedEvent.cs`
- Create: `src/BaseCleanArchitecture.Domain/Events/User/UserLoggedInEvent.cs`

- [ ] **Step 1: Create User entity**

```csharp
using BaseCleanArchitecture.Domain.Abtractions;

namespace BaseCleanArchitecture.Domain.Entities;

public class User : EntityAuditBase<Guid>
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Salt { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public bool IsPhoneNumberConfirmed { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiry { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
```

- [ ] **Step 2: Create Role entity**

```csharp
using BaseCleanArchitecture.Domain.Abtractions;

namespace BaseCleanArchitecture.Domain.Entities;

public class Role : EntityAuditBase<Guid>
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
```

- [ ] **Step 3: Create UserRole join entity**

```csharp
using BaseCleanArchitecture.Domain.Abtractions;

namespace BaseCleanArchitecture.Domain.Entities;

public class UserRole : EntityAuditBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
```

- [ ] **Step 4: Create UserCreatedEvent**

```csharp
using BaseCleanArchitecture.Domain.Abtractions;

namespace BaseCleanArchitecture.Domain.Events.User;

public class UserCreatedEvent : DomainEventBase
{
    public Guid UserId { get; }
    public string Username { get; }
    public string Email { get; }

    public UserCreatedEvent(Guid userId, string username, string email)
    {
        UserId = userId;
        Username = username;
        Email = email;
    }
}
```

- [ ] **Step 5: Create UserLoggedInEvent**

```csharp
using BaseCleanArchitecture.Domain.Abtractions;

namespace BaseCleanArchitecture.Domain.Events.User;

public class UserLoggedInEvent : DomainEventBase
{
    public Guid UserId { get; }
    public string Username { get; }
    public DateTimeOffset LoggedInAt { get; }

    public UserLoggedInEvent(Guid userId, string username)
    {
        UserId = userId;
        Username = username;
        LoggedInAt = DateTimeOffset.UtcNow;
    }
}
```

- [ ] **Step 6: Build to verify Domain project compiles**

```bash
dotnet build src/BaseCleanArchitecture.Domain/BaseCleanArchitecture.Domain.csproj
```

Expected: Build succeeded.

---

### Task 2: Application Interfaces + Models

**Files:**
- Modify: `src/BaseCleanArchitecture.Application/Common/Interfaces/IJwtTokenService.cs`
- Create: `src/BaseCleanArchitecture.Application/Common/Interfaces/IAuthService.cs`
- Create: `src/BaseCleanArchitecture.Application/Common/Interfaces/IPasswordHasher.cs`
- Modify: `src/BaseCleanArchitecture.Application/Common/Models/Result.cs`
- Modify: `src/BaseCleanArchitecture.Application/Common/Models/CurrentUser.cs`
- Create: `src/BaseCleanArchitecture.Application/Common/Models/AuthTokenDto.cs`
- Create: `src/BaseCleanArchitecture.Application/Common/Models/RegisterDto.cs`

- [ ] **Step 1: Fix IJwtTokenService — class → interface**

Replace the entire file:

```csharp
using BaseCleanArchitecture.Domain.Entities;

namespace BaseCleanArchitecture.Application.Common.Interfaces;

public interface IJwtTokenService
{
    AuthTokenDto GenerateToken(User user);
    ClaimsPrincipal? ValidateToken(string token);
    string GenerateRefreshToken();
}
```

Note: `AuthTokenDto` is in `BaseCleanArchitecture.Application.Common.Models` (created in Step 5).

- [ ] **Step 2: Create IAuthService**

```csharp
using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Domain.Entities;

namespace BaseCleanArchitecture.Application.Common.Interfaces;

public interface IAuthService
{
    Task<User> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<User> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Create IPasswordHasher**

```csharp
namespace BaseCleanArchitecture.Application.Common.Interfaces;

public interface IPasswordHasher
{
    (string hash, string salt) Hash(string password);
    bool Verify(string password, string hash, string salt);
}
```

- [ ] **Step 4: Fix Result.cs — immutable sealed record**

Replace the entire file:

```csharp
namespace BaseCleanArchitecture.Application.Common.Models;

public interface IResult
{
    int StatusCode { get; }
    bool IsSuccess { get; }
    string? Message { get; }
}

public interface IResult<out T> : IResult
{
    T? Data { get; }
}

public sealed record Result : IResult
{
    public int StatusCode { get; }
    public bool IsSuccess { get; }
    public string? Message { get; }

    private Result(int statusCode, bool isSuccess, string? message)
    {
        StatusCode = statusCode;
        IsSuccess = isSuccess;
        Message = message;
    }

    public static Result Success(int statusCode = 200, string? message = null)
        => new(statusCode, true, message);

    public static Result Failure(int statusCode, string? message)
        => new(statusCode, false, message);
}

public sealed record Result<T> : IResult<T>
{
    public int StatusCode { get; }
    public bool IsSuccess { get; }
    public string? Message { get; }
    public T? Data { get; }

    private Result(int statusCode, bool isSuccess, T? data, string? message)
    {
        StatusCode = statusCode;
        IsSuccess = isSuccess;
        Message = message;
        Data = data;
    }

    public static Result<T> Success(T data, int statusCode = 200, string? message = null)
        => new(statusCode, true, data, message);

    public static Result<T> Failure(int statusCode, string? message)
        => new(statusCode, false, default, message);
}
```

- [ ] **Step 5: Create AuthTokenDto**

```csharp
namespace BaseCleanArchitecture.Application.Common.Models;

public sealed record AuthTokenDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);
```

- [ ] **Step 6: Create RegisterDto**

```csharp
namespace BaseCleanArchitecture.Application.Common.Models;

public sealed record RegisterDto(
    string Username,
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber = null
);
```

- [ ] **Step 7: Update CurrentUser — add helper methods**

Replace the entire file:

```csharp
namespace BaseCleanArchitecture.Application.Common.Models;

public class CurrentUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Permissions { get; set; } = string.Empty;

    public bool IsAuthenticated => Id != Guid.Empty;

    public bool IsAdmin() => Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    public bool IsCustomer() => Role.Equals("Customer", StringComparison.OrdinalIgnoreCase);
    public bool IsInRole(string role) => Role.Equals(role, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 8: Build to verify Application project compiles**

```bash
dotnet build src/BaseCleanArchitecture.Application/BaseCleanArchitecture.Application.csproj
```

Expected: Build succeeded.

---

### Task 3: Infrastructure — JwtTokenService + PasswordHasher + DI

**Files:**
- Modify: `src/BaseCleanArchitecture.Infrastructure/BaseCleanArchitecture.Infrastructure.csproj`
- Modify: `src/BaseCleanArchitecture.Infrastructure/Auth/JwtOptions.cs`
- Create: `src/BaseCleanArchitecture.Infrastructure/Auth/PasswordHasher.cs`
- Create: `src/BaseCleanArchitecture.Infrastructure/Auth/JwtTokenService.cs`
- Create: `src/BaseCleanArchitecture.Infrastructure/Auth/AuthDependencyInjection.cs`
- Modify: `src/BaseCleanArchitecture.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: Add JwtBearer package to Infrastructure.csproj**

Add inside `<ItemGroup>` with other packages:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.2" />
```

- [ ] **Step 2: Fix JwtOptions — add SectionName, set → init**

Replace the entire file:

```csharp
namespace BaseCleanArchitecture.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public int ExpiryMinutes { get; init; } = 60;
}
```

- [ ] **Step 3: Create PasswordHasher**

```csharp
using System.Security.Cryptography;
using BaseCleanArchitecture.Application.Common.Interfaces;

namespace BaseCleanArchitecture.Infrastructure.Auth;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 32;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public (string hash, string salt) Hash(string password)
    {
        var saltBytes = new byte[SaltSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);

        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public bool Verify(string password, string hash, string salt)
    {
        var hashBytes = Convert.FromBase64String(hash);
        var saltBytes = Convert.FromBase64String(salt);

        var computedBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return CryptographicOperations.FixedTimeEquals(computedBytes, hashBytes);
    }
}
```

- [ ] **Step 4: Create JwtTokenService**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BaseCleanArchitecture.Infrastructure.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AuthTokenDto GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email)
        };

        // Add role claims
        if (user.UserRoles is { Count: > 0 })
        {
            foreach (var userRole in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole.Role?.Name ?? string.Empty));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(securityToken);

        var refreshToken = GenerateRefreshToken();

        return new AuthTokenDto(accessToken, refreshToken, expiresAt);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_options.SecretKey);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
```

- [ ] **Step 5: Create AuthDependencyInjection**

```csharp
using System.Text;
using BaseCleanArchitecture.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BaseCleanArchitecture.Infrastructure.Auth;

public static class AuthDependencyInjection
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind JwtOptions
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException($"'{JwtOptions.SectionName}' section is missing or invalid.");

        // Register auth services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Configure JWT authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        return services;
    }
}
```

- [ ] **Step 6: Update Infrastructure/DependencyInjection.cs — call AddAuthServices**

Add the call at the top of `AddInfrastructure` (before messaging):

```csharp
using BaseCleanArchitecture.Infrastructure.Auth;
```

And in the method body, add as first line:

```csharp
services.AddAuthServices(configuration);
```

The full updated method:

```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, ILoggingBuilder loggingBuilder)
{
    // Auth (JWT)
    services.AddAuthServices(configuration);

    // Register infrastructure services here
    services.AddMessagingConfiguration(configuration);

    // Email Service
    services.AddEmailServiceConfiguration(configuration);

    // Caching
    services.AddCachingConfiguration();

    // Logging
    loggingBuilder.AddOpenTelemetryLogging();

    // Domain Event Dispatcher
    services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

    return services;
}
```

- [ ] **Step 7: Build to verify Infrastructure project compiles**

```bash
dotnet build src/BaseCleanArchitecture.Infrastructure/BaseCleanArchitecture.Infrastructure.csproj
```

Expected: Build succeeded. If `ClaimTypes` not found, add `using System.Security.Claims;` to `AuthDependencyInjection.cs`.

---

### Task 4: CQRS Commands + AuthService + API DTOs

**Files:**
- Create: `src/BaseCleanArchitecture.Application/Features/Auth/Models/RegisterRequest.cs`
- Create: `src/BaseCleanArchitecture.Application/Features/Auth/Models/LoginRequest.cs`
- Create: `src/BaseCleanArchitecture.Application/Features/Auth/Commands/RegisterCommand.cs`
- Create: `src/BaseCleanArchitecture.Application/Features/Auth/Commands/LoginCommand.cs`
- Create: `src/BaseCleanArchitecture.Application/Services/AuthService.cs`
- Modify: `src/BaseCleanArchitecture.Application/DependencyInjection.cs`

- [ ] **Step 1: Create RegisterRequest (API DTO)**

```csharp
namespace BaseCleanArchitecture.Application.Features.Auth.Models;

public sealed record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber = null
);
```

- [ ] **Step 2: Create LoginRequest (API DTO)**

```csharp
namespace BaseCleanArchitecture.Application.Features.Auth.Models;

public sealed record LoginRequest(
    string Username,
    string Password
);
```

- [ ] **Step 3: Create RegisterCommand + Handler**

```csharp
using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Application.Common.Models;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.Auth.Commands;

public sealed record RegisterCommand : IRequest<Result<AuthTokenDto>>
{
    public string Username { get; }
    public string Email { get; }
    public string Password { get; }
    public string FullName { get; }
    public string? PhoneNumber { get; }

    public RegisterCommand(string username, string email, string password, string fullName, string? phoneNumber)
    {
        Username = username;
        Email = email;
        Password = password;
        FullName = fullName;
        PhoneNumber = phoneNumber;
    }
}

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthTokenDto>>
{
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterCommandHandler(IAuthService authService, IJwtTokenService jwtTokenService)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthTokenDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var dto = new RegisterDto(request.Username, request.Email, request.Password, request.FullName, request.PhoneNumber);
        var user = await _authService.RegisterAsync(dto, cancellationToken);
        var token = _jwtTokenService.GenerateToken(user);
        return Result<AuthTokenDto>.Success(token);
    }
}
```

- [ ] **Step 4: Create LoginCommand + Handler**

```csharp
using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Application.Common.Models;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.Auth.Commands;

public sealed record LoginCommand : IRequest<Result<AuthTokenDto>>
{
    public string Username { get; }
    public string Password { get; }

    public LoginCommand(string username, string password)
    {
        Username = username;
        Password = password;
    }
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
{
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(IAuthService authService, IJwtTokenService jwtTokenService)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthTokenDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _authService.LoginAsync(request.Username, request.Password, cancellationToken);
        var token = _jwtTokenService.GenerateToken(user);
        return Result<AuthTokenDto>.Success(token);
    }
}
```

- [ ] **Step 5: Create AuthService**

```csharp
using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Events.User;

namespace BaseCleanArchitecture.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IRepositoryBase<User, Guid> _userRepository;
    private readonly IRepositoryBase<UserRole, Guid> _userRoleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IRepositoryBase<User, Guid> userRepository,
        IRepositoryBase<UserRole, Guid> userRoleRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<User> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        var (hash, salt) = _passwordHasher.Hash(dto.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = hash,
            Salt = salt,
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            IsEmailConfirmed = false,
            IsPhoneNumberConfirmed = false,
            IsTwoFactorEnabled = false
        };

        // Assign Customer role by default
        var customerRoleId = new Guid("22222222-2222-2222-2222-222222222222");
        user.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RoleId = customerRoleId
        });

        // Raise domain event
        user.AddDomainEvent(new UserCreatedEvent(user.Id, user.Username, user.Email));

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<User> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        // Find user — need to implement FindByUsername. For now use GetAllAsync + LINQ then filter.
        // This will be replaced with proper repository method after Task 5.
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var user = users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Invalid username or password.");

        if (!_passwordHasher.Verify(password, user.PasswordHash, user.Salt))
            throw new InvalidOperationException("Invalid username or password.");

        user.LastLoginAt = DateTimeOffset.UtcNow;

        // Raise domain event
        user.AddDomainEvent(new UserLoggedInEvent(user.Id, user.Username));

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user;
    }
}
```

**Note on LoginAsync:** The `UserLoggedInEvent` is raised and dispatched. The `User` method `AddDomainEvent` is `protected` in `EntityBase<TKey>`, so the auth service cannot call it directly. Since this is a domain behavior, the event raising should happen inside the entity method or the handler. For this plan, we'll use the `DomainEvents` list reflection approach, or better: we'll add a public method to User. Let me adjust:

**Alternative for LoginAsync — use a helper to add domain event:**

The correct approach per Clean Architecture: the domain event should be raised inside an entity behavior method. Add a `RecordLogin` method to User entity:

Add to User.cs (in Task 1):

```csharp
public void RecordLogin()
{
    LastLoginAt = DateTimeOffset.UtcNow;
    AddDomainEvent(new UserLoggedInEvent(Id, Username));
}
```

Then in AuthService.LoginAsync:

```csharp
user.RecordLogin();
await _userRepository.UpdateAsync(user, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

**Updated User.cs should include `RecordLogin`.** Will be handled in Task 1.

- [ ] **Step 6: Update Application DependencyInjection — register AuthService**

Replace the file:

```csharp
using BaseCleanArchitecture.Application.Services;
using BaseCleanArchitecture.Domain.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BaseCleanArchitecture.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        // Register application services here
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<Common.Interfaces.IAuthService, AuthService>();
        return services;
    }
}
```

- [ ] **Step 7: Build to verify Application project compiles**

```bash
dotnet build src/BaseCleanArchitecture.Application/BaseCleanArchitecture.Application.csproj
```

Expected: Build succeeded. Note: `User.AddDomainEvent` and `User.AddDomainEvent` may show errors if not yet added to User entity — ensure Task 1 included them.

---

### Task 5: Persistence — EF Configurations + DbContext + CurrentUserService

**Files:**
- Create: `src/BaseCleanArchitecture.Persistence/Configurations/UserConfiguration.cs`
- Create: `src/BaseCleanArchitecture.Persistence/Configurations/RoleConfiguration.cs`
- Create: `src/BaseCleanArchitecture.Persistence/Configurations/UserRoleConfiguration.cs`
- Modify: `src/BaseCleanArchitecture.Persistence/ApplicationDbContext.cs`
- Modify: `src/BaseCleanArchitecture.Persistence/CurrentUserService.cs`
- Modify: `src/BaseCleanArchitecture.Persistence/DependencyInjection.cs`

- [ ] **Step 1: Create UserConfiguration**

```csharp
using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BaseCleanArchitecture.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Salt)
            .IsRequired();

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(500);
    }
}
```

- [ ] **Step 2: Create RoleConfiguration with seed data**

```csharp
using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BaseCleanArchitecture.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.Name)
            .IsUnique();

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Description)
            .HasMaxLength(200);

        // Seed data
        builder.HasData(
            new Role
            {
                Id = new Guid("11111111-1111-1111-1111-111111111111"),
                Name = "Admin",
                Description = "Administrator role with full access",
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                IsDeleted = false
            },
            new Role
            {
                Id = new Guid("22222222-2222-2222-2222-222222222222"),
                Name = "Customer",
                Description = "Default customer role",
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                IsDeleted = false
            }
        );
    }
}
```

- [ ] **Step 3: Create UserRoleConfiguration**

```csharp
using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BaseCleanArchitecture.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(ur => ur.Id);

        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique();

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 4: Update ApplicationDbContext — add DbSets**

Add new DbSets:

```csharp
public DbSet<User> Users { get; set; }
public DbSet<Role> Roles { get; set; }
public DbSet<UserRole> UserRoles { get; set; }
```

Full updated file:

```csharp
using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Domain.Abtractions;
using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BaseCleanArchitecture.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IDomainEventDispatcher _domainEventDispatcher;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUserService, IDomainEventDispatcher domainEventDispatcher) : base(options)
        {
            _currentUserService = currentUserService;
            _domainEventDispatcher = domainEventDispatcher;
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<EntityAuditBase<Guid>>();

            var entitiesWithEvents = ChangeTracker
                                .Entries<EntityAuditBase<Guid>>()
                                .Where(e => e.Entity.DomainEvents.Any())
                                .Select(e => e.Entity)
                                .ToList();

            var domainEvents = entitiesWithEvents
                .SelectMany(e => e.DomainEvents)
                .ToList();

            entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                    entry.Entity.CreatedBy ??= _currentUserService.CurrentUser.Id;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                    entry.Entity.UpdatedBy ??= _currentUserService.CurrentUser.Id;
                }
            }

            var result = await base.SaveChangesAsync(cancellationToken);

            if (result > 0 && domainEvents.Any())
            {
                await _domainEventDispatcher.PublishEventsAsync(
                    domainEvents,
                    cancellationToken);
            }

            return result;
        }
    }
}
```

- [ ] **Step 5: Update CurrentUserService — extract from HttpContext**

Replace the entire file:

```csharp
using System.Security.Claims;
using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Application.Common.Models;
using Microsoft.AspNetCore.Http;

namespace BaseCleanArchitecture.Persistence;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUser CurrentUser => ExtractFromClaims();

    private CurrentUser ExtractFromClaims()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal is not { Identity.IsAuthenticated: true })
            return new CurrentUser();

        return new CurrentUser
        {
            Id = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty,
            Username = principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            Role = principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty
        };
    }
}
```

- [ ] **Step 6: Update Persistence DependencyInjection — add HttpContextAccessor + repository registrations**

Add `using Microsoft.AspNetCore.Http;` and `using BaseCleanArchitecture.Domain.Entities;` at top, then add:

```csharp
services.AddHttpContextAccessor();
services.AddScoped<Domain.Abtractions.Repositories.IRepositoryBase<User, Guid>, Repositories.RepositoryBase<User, Guid>>();
services.AddScoped<Domain.Abtractions.Repositories.IRepositoryBase<UserRole, Guid>, Repositories.RepositoryBase<UserRole, Guid>>();
```

Full updated file:

```csharp
using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Persistence.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaseCleanArchitecture.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddTransient<IUnitOfWork, UnitOfWork>();
        services.AddTransient<ICurrentUserService, CurrentUserService>();
        services.AddHttpContextAccessor();

        // Register repositories
        services.AddScoped<IRepositoryBase<User, Guid>, RepositoryBase<User, Guid>>();
        services.AddScoped<IRepositoryBase<UserRole, Guid>, RepositoryBase<UserRole, Guid>>();

        return services;
    }
}
```

- [ ] **Step 7: Build to verify Persistence project compiles**

```bash
dotnet build src/BaseCleanArchitecture.Persistence/BaseCleanArchitecture.Persistence.csproj
```

Expected: Build succeeded.

---

### Task 6: WebAPI — AuthController + Program.cs + appsettings

**Files:**
- Create: `src/BaseCleanArchitecture.WebAPI/Controllers/Features/AuthController.cs`
- Modify: `src/BaseCleanArchitecture.WebAPI/Program.cs`
- Modify: `src/BaseCleanArchitecture.WebAPI/appsettings.json`
- Modify: `src/BaseCleanArchitecture.WebAPI/appsettings.Development.json`

- [ ] **Step 1: Create AuthController**

```csharp
using BaseCleanArchitecture.Application.Features.Auth.Commands;
using BaseCleanArchitecture.Application.Features.Auth.Models;
using BaseCleanArchitecture.WebAPI.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BaseCleanArchitecture.WebAPI.Controllers.Features;

[Route("api/[controller]")]
[ApiController]
public class AuthController : BaseController
{
    public AuthController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(
            request.Username,
            request.Email,
            request.Password,
            request.FullName,
            request.PhoneNumber);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Username, request.Password);
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
```

- [ ] **Step 2: Update Program.cs — add UseAuthentication + new DI using**

Add using:
```csharp
using BaseCleanArchitecture.Infrastructure.Auth;
```

Add `app.UseAuthentication();` BEFORE `app.UseAuthorization();`:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

The full updated Program.cs (key changes shown):

```csharp
using BaseCleanArchitecture.Application;
using BaseCleanArchitecture.Application.Behaviors;
using BaseCleanArchitecture.Infrastructure;
using BaseCleanArchitecture.Infrastructure.Auth;
using BaseCleanArchitecture.Persistence;
using BaseCleanArchitecture.WebAPI.Middleware;
using MediatR;
using Scalar.AspNetCore;
using Serilog;
using System.Reflection;

// ... (rest of Program.cs unchanged until app configuration) ...

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("BaseCleanArchitecture API")
            .WithTheme(ScalarTheme.Mars)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

app.Run();
```

- [ ] **Step 3: Update appsettings.json — add Jwt section**

Add after `"AllowedHosts": "*"`:

```json
"Jwt": {
  "Issuer": "BaseCleanArchitecture",
  "Audience": "BaseCleanArchitecture",
  "SecretKey": "super-secret-key-with-minimum-32-characters-long!!",
  "ExpiryMinutes": 60
}
```

- [ ] **Step 4: Update appsettings.Development.json — add Jwt section**

Add after `"AllowedHosts": "*"`:

```json
"Jwt": {
  "Issuer": "BaseCleanArchitecture",
  "Audience": "BaseCleanArchitecture",
  "SecretKey": "dev-secret-key-at-least-32-characters-long!!",
  "ExpiryMinutes": 60
}
```

- [ ] **Step 5: Build entire solution**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors.

---

## Execution Order

```
Task 1 (Domain) ─────────────────┐
                                  ├─→ Task 2 (Application interfaces)
                                  │         │
                                  │    ┌────┼────────┐
                                  │    │    │        │
                                  ▼    ▼    ▼        ▼
                              Task 3  Task 4  Task 5  (parallel batch)
                              (Infra) (CQRS) (Persist)
                                  │    │    │
                                  └────┼────┘
                                       ▼
                                   Task 6 (WebAPI)
```

**Parallel dispatch:** Tasks 3, 4, and 5 can be dispatched together after Task 2 completes.
