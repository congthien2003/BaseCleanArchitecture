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
