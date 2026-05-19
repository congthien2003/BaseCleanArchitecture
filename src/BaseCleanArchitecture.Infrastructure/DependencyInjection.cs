using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Application.Interfaces;
using BaseCleanArchitecture.Infrastructure.Caching;
using BaseCleanArchitecture.Infrastructure.Email;
using BaseCleanArchitecture.Infrastructure.Messaging;
using BaseCleanArchitecture.Infrastructure.Services.Events;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaseCleanArchitecture.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Register infrastructure services here
            services.AddMessagingConfiguration(configuration);

            // Email Service
            services.AddEmailServiceConfiguration(configuration);

            // Caching
            services.AddCachingConfiguration();

            // Domain Event Dispatcher
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            return services;
        }
    }
}
