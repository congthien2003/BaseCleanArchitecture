using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Application.Features.Auth.Models;
using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Events.User;

namespace BaseCleanArchitecture.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRepositoryBase<Role, Guid> _roleRepository;
    private readonly IRepositoryBase<UserRole, Guid> _userRoleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository userRepository,
        IRepositoryBase<Role, Guid> roleRepository,
        IRepositoryBase<UserRole, Guid> userRoleRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<User> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        var (hash, salt) = _passwordHasher.Hash(dto.Password);

        // Query Customer role by name instead of hardcoded GUID
        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        var customerRole = roles.FirstOrDefault(r => r.Name == "Customer")
            ?? throw new InvalidOperationException("Customer role not found. Ensure seed data has been applied.");

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

        user.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RoleId = customerRole.Id
        });
        user.RecordRegister();

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<User> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByUsernameAsync(username, cancellationToken)
            ?? throw new InvalidOperationException("Invalid username or password.");

        if (!_passwordHasher.Verify(password, user.PasswordHash, user.Salt))
            throw new InvalidOperationException("Invalid username or password.");

        user.RecordLogin();

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user;
    }
}
