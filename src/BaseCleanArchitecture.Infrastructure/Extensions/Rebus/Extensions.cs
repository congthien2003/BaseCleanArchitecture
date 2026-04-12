using BaseCleanArchitecture.Infrastructure.Messaging.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rebus.Config;

namespace BaseCleanArchitecture.Infrastructure.Extensions.Rebus
{
    public static class Extensions
    {
        public static void AddRebusServices(this IServiceCollection services, RabbitMQOptions options)
        {
            services.AddRebus(configure =>
            {
                var client = $"amqp://{options.UserName}:{options.Password}@{options.HostName}:{options.Port}";
                var configurer = configure
                    .Logging(l => l.ColoredConsole())
                    .Transport(t => t.UseRabbitMqAsOneWayClient(client));

                return configurer;
            });
        }
    }
}
