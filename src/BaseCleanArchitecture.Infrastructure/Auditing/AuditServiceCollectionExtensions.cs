using BaseCleanArchitecture.Application.Abstractions.Auditing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaseCleanArchitecture.Infrastructure.Auditing
{
    public static class AuditServiceCollectionExtensions
    {
        public static IServiceCollection AddAuditServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<AuditOptions>()
                .Bind(configuration.GetSection(AuditOptions.SectionName));

            // The app must not start without a usable key: better no app than plaintext or silently lost audit logs.
            services.AddOptions<AuditEncryptionOptions>()
                .Bind(configuration.GetSection(AuditEncryptionOptions.SectionName))
                .Validate(
                    o => AuditEncryptionOptions.IsValidKey(o.Key),
                    $"{AuditEncryptionOptions.SectionName}:Key must be a base64 string of {AuditEncryptionOptions.KeySizeBytes} bytes.")
                .ValidateOnStart();

            services.AddSingleton<IAuditEncryptor, AesGcmAuditEncryptor>();

            services.AddScoped<IAuditContext, AuditContext>();

            services.AddSingleton<AuditLogChannel>();
            services.AddSingleton<IAuditLogPublisher>(sp => sp.GetRequiredService<AuditLogChannel>());
            services.AddHostedService<AuditLogWorker>();

            return services;
        }
    }
}
