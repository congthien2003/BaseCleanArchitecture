using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Domain.Abtractions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BaseCleanArchitecture.Infrastructure.Services.Events
{
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DomainEventDispatcher> _logger;
        private readonly IPublisher _publisher;

        public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger, IPublisher publisher)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _publisher = publisher;
        }

        public async Task PublishAsync(DomainEventBase domainEvent, CancellationToken cancellationToken = default)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }

        public async Task PublishEventsAsync(IEnumerable<DomainEventBase> domainEvents, CancellationToken cancellationToken = default)
        {
            foreach (var domainEvent in domainEvents)
            {
                await PublishAsync(domainEvent, cancellationToken);
            }
        }
    }
}
