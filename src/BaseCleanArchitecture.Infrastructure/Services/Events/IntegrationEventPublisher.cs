using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Contract.ExternalEvents.Abstractions;
using Microsoft.Extensions.Logging;
using Rebus.Bus;

namespace BaseCleanArchitecture.Infrastructure.Services.Events
{
    public class IntegrationEventPublisher : IMessagingService
    {
        private readonly ILogger<IntegrationEventPublisher> _logger;
        private readonly IBus _bus;

        public IntegrationEventPublisher(ILogger<IntegrationEventPublisher> logger, IBus bus)
        {
            _logger = logger;
            _bus = bus;
        }

        public async Task Publish<T>(T @event, CancellationToken cancellationToken) where T : BaseExternalEvent
        {
            _logger.LogInformation("Publishing integration event: {EventId} at {OccurredOn}", @event.EventId, @event.OccurredOn);

            await _bus.Publish(@event);
        }
    }
}
