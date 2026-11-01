using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaseCleanArchitecture.Infrastructure.Security
{
    public static class AdaptiveSecurityExtensions
    {
        public static IServiceCollection AddAdaptiveSecurity(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<AdaptiveSecurityOptions>()
                .Bind(configuration.GetSection(AdaptiveSecurityOptions.SectionName))
                .Validate(
                    o => o.NotFoundThreshold > 0 && o.NotFoundWindowSeconds > 0 && o.BlockDurationMinutes > 0,
                    $"{AdaptiveSecurityOptions.SectionName}: threshold, window and block duration must be positive.")
                .ValidateOnStart();

            services.AddMemoryCache();
            services.AddSingleton<IIpBlockList, MemoryIpBlockList>();

            return services;
        }
    }
}
