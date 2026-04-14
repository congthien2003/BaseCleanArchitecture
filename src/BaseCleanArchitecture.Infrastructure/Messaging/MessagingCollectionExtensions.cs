using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Infrastructure.Messaging.Kafka;
using BaseCleanArchitecture.Infrastructure.Messaging.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace BaseCleanArchitecture.Infrastructure.Messaging
{
    public static class MessagingCollectionExtensions
    {
        public static void AddMessagingConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Lấy section từ appsettings
            var messagingSection = configuration.GetSection(MessagingOptions.SectionName);

            // 2. Đăng ký Options Pattern (Để inject IOptions<MessagingOptions> ở bất kỳ đâu nếu cần)
            services.Configure<MessagingOptions>(messagingSection);

            // 3. Đọc tạm giá trị config để xử lý logic DI
            var messagingOptions = messagingSection.Get<MessagingOptions>();

            if (messagingOptions == null)
            {
                throw new InvalidOperationException($"Failed to bind '{MessagingOptions.SectionName}' section to {nameof(MessagingOptions)}.");
            }

            if (messagingOptions.UseRabbitMQ)
            {
                Console.WriteLine("Use RabbitMQ for Messaging");
                services.AddRebus(configure =>
                {
                    var client = $"amqp://{messagingOptions.RabbitMQOptions.UserName}:{messagingOptions.RabbitMQOptions.Password}@{messagingOptions.RabbitMQOptions.HostName}:{messagingOptions.RabbitMQOptions.Port}";
                    var configurer = configure
                        .Logging(l => l.ColoredConsole())
                        .Transport(t => t.UseRabbitMqAsOneWayClient(client));

                    return configurer;
                });
                services.AddSingleton<IMessagingService, RabbitMQMessagingService>();
            } else if (messagingOptions.UseKafka)
            {
                Console.WriteLine("Use Kafka for Messaging");
                services.AddSingleton<IMessagingService, KafkaMessagingService>();
            }
            else
            {
                throw new NotSupportedException($"Messaging provider '{messagingOptions.Provider}' is not supported.");
            }
        }
    }
}
