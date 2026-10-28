using BaseCleanArchitecture.Domain.Abtractions;
using BaseCleanArchitecture.Domain.Enums;
using BaseCleanArchitecture.Domain.Events.User;

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
    public DateOnly? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Unknown;
    public string? Address { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public User() : base(Guid.CreateVersion7())
    {
    }

    public static User CreateUser(string userName, string email, string fullName, string phoneNumber, string hash, string salt)
    {
        var user = new User
        {
            Username = userName,
            Email = email,
            PasswordHash = hash,
            Salt = salt,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            IsEmailConfirmed = false,
            IsPhoneNumberConfirmed = false,
            IsTwoFactorEnabled = false
        };

        return user;
    }



    public bool CanLogin => !IsDeleted && Status == UserStatus.Active;

    public void SetPassword(string hash, string salt)
    {
        PasswordHash = hash;
        Salt = salt;
    }

    /// <summary>
    /// Records a login event — updates LastLoginAt and raises UserLoggedInEvent
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new UserLoggedInEvent(Id, Username));
    }

    /// <summary>
    /// Records a registration event — raises UserCreatedEvent
    /// </summary>
    public void RecordRegister()
    {
        AddDomainEvent(new UserCreatedEvent(Id, Username, Email));
    }
}
