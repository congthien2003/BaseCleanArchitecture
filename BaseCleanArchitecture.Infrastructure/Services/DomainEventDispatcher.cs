using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Domain.Abtractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BaseCleanArchitecture.Infrastructure.Services
{
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DomainEventDispatcher> _logger;

        public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task PublishAsync(DomainEventBase domainEvent, CancellationToken cancellationToken = default)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

            using var scope = _serviceProvider.CreateScope();
            var handlers = scope.ServiceProvider.GetServices(handlerType);

            if (!handlers.Any())
            {
                _logger.LogWarning(
                    "No handlers found for domain event {EventType}",
                    eventType.Name);
                return;
            }

            foreach (dynamic handler in handlers)
            {
                try
                {
                    await handler.Handle((dynamic)domainEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error handling domain event {EventType}",
                        eventType.Name);
                    throw;
                }
            }
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
