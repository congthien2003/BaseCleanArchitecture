using BaseCleanArchitecture.Application.Abstractions.Infrastructures;
using BaseCleanArchitecture.Contract.ExternalEvents.Abstractions;
using Microsoft.Extensions.Logging;
using Rebus.Bus;

namespace BaseCleanArchitecture.Infrastructure.Messaging.RabbitMQ
{
    public class RabbitMQMessagingService : IMessagingService
    {
        private readonly ILogger<RabbitMQMessagingService> _logger;
        private readonly IBus _bus;

        public RabbitMQMessagingService(ILogger<RabbitMQMessagingService> logger, IBus bus)
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
