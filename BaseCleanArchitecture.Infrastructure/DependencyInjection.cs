using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Infrastructure.Extensions.Rebus;
using BaseCleanArchitecture.Infrastructure.Services.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaseCleanArchitecture.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Register infrastructure services here
            services.AddRebusServices(configuration);

            // Domain Event Dispatcher
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            return services;
        }
    }
}
