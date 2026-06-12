using BaseCleanArchitecture.Application.Abstractions.Infrastructures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaseCleanArchitecture.Infrastructure.Email
{
    public static class EmailExtension
    {
        public static void AddEmailServiceConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var emailSection = configuration.GetSection(nameof(EmailOptions));

            services.Configure<EmailOptions>(emailSection);

            var emailOptions = emailSection.Get<EmailOptions>();

            if (emailOptions is null)
            {
                throw new InvalidOperationException("Email options are not configured properly.");
            }

            services.AddScoped<IEmailService, EmailService>();
        }
    }
}
