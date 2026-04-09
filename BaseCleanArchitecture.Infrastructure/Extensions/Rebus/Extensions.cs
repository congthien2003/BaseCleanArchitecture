using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Config;

namespace BaseCleanArchitecture.Infrastructure.Extensions.Rebus
{
    public static class Extensions
    {
        public static void AddRebusServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddRebus(configure =>
            {
                var configurer = configure
                    .Logging(l => l.ColoredConsole())
                    .Transport(t => t.UseRabbitMqAsOneWayClient("amqp://guest:guest@localhost:5672"));

                return configurer;
            });
        }
    }
}
