using BaseCleanArchitecture.Domain.Abtractions;
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
    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiry { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

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
