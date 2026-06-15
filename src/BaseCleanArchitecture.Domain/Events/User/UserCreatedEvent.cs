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
