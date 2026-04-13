using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Contract.ExternalEvents.Abstractions;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BaseCleanArchitecture.Infrastructure.Messaging.Kafka
{
    public class KafkaMessagingService : IMessagingService
    {
        private readonly ILogger<KafkaMessagingService> _logger;
        private readonly KafkaOptions _kafkaOptions;
        private readonly IProducer<Null, string> _producer;

        public KafkaMessagingService(ILogger<KafkaMessagingService> logger, IOptions<MessagingOptions> options)
        {
            _logger = logger;
            _kafkaOptions = options.Value.KafkaOptions ?? throw new ArgumentNullException(nameof(options.Value.KafkaOptions), "KafkaOptions must be provided in configuration.");

            var config = new ProducerConfig
            {
                BootstrapServers = _kafkaOptions.BootstrapServers,
                Acks = Acks.All,
            };

            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task Publish<T>(T @event, CancellationToken cancellationToken) where T : BaseExternalEvent
        {
            _logger.LogInformation("Publishing event {EventId} of type {EventType} to Kafka", @event.EventId, @event.GetType().Name);

            var topic = typeof(T).Name ?? _kafkaOptions.Topic;
            var payload = JsonSerializer.Serialize(@event);

            var kafkaMessage = new Message<Null, string> { Value = payload };

            await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
        }
    }
}
