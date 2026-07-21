using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Abstractions.Infrastructures;
using BaseCleanArchitecture.Infrastructure.Auth;
using BaseCleanArchitecture.Infrastructure.Caching;
using BaseCleanArchitecture.Infrastructure.Email;
using BaseCleanArchitecture.Infrastructure.Messaging;
using BaseCleanArchitecture.Infrastructure.OpenTelemetry;
using BaseCleanArchitecture.Infrastructure.Services.Events;
using BaseCleanArchitecture.Persistence;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BaseCleanArchitecture.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, ILoggingBuilder loggingBuilder)
        {
            // Auth (JWT)
            services.AddAuthServices(configuration);

            // Register infrastructure services here
            //services.AddMessagingConfiguration(configuration);

            // Email Service
            services.AddEmailServiceConfiguration(configuration);

            // Caching
            services.AddCachingConfiguration();

            // Logging
            loggingBuilder.AddOpenTelemetryLogging();

            // Domain Event Dispatcher
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            services.AddTransient<ICurrentUserService, CurrentUserService>();
            services.AddHttpContextAccessor();

            return services;
        }
    }
}
