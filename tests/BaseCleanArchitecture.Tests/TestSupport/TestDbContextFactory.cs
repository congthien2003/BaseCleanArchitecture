using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Abstractions.Infrastructures;
using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Domain.Abtractions;
using BaseCleanArchitecture.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BaseCleanArchitecture.Tests.TestSupport;

public static class TestDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, new FakeCurrentUserService(), new FakeDomainEventDispatcher());
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public CurrentUser CurrentUser { get; } = new();
    }

    private sealed class FakeDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task PublishAsync(DomainEventBase domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishEventsAsync(IEnumerable<DomainEventBase> domainEvents, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
