using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Infrastructure.Extensions.Rebus;
using BaseCleanArchitecture.Infrastructure.Messaging.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                services.AddRebusServices(messagingOptions.RabbitMQOptions);
                services.AddSingleton<IMessagingService, RabbitMQMessagingService>();
            }
            else
            {
                throw new NotSupportedException($"Messaging provider '{messagingOptions.Provider}' is not supported.");
            }
        }
    }
}
