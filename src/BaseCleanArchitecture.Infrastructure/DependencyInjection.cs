using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Application.Interfaces;
using BaseCleanArchitecture.Infrastructure.Caching;
using BaseCleanArchitecture.Infrastructure.Email;
using BaseCleanArchitecture.Infrastructure.Messaging;
using BaseCleanArchitecture.Infrastructure.OpenTelemetry;
using BaseCleanArchitecture.Infrastructure.Services.Events;
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
            // Register infrastructure services here
            services.AddMessagingConfiguration(configuration);

            // Email Service
            services.AddEmailServiceConfiguration(configuration);

            // Caching
            services.AddCachingConfiguration();

            // OpenTelemetry — tracing, metrics, and logging (OTLP + Prometheus)
            services.AddOpenTelemetryObservability(loggingBuilder, configuration);

            // Domain Event Dispatcher
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            return services;
        }
    }
}
