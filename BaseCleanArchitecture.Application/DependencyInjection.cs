using BaseCleanArchitecture.Application.Behaviors;
using BaseCleanArchitecture.Application.Services;
using BaseCleanArchitecture.Domain.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.NetworkInformation;
using System.Reflection;


namespace BaseCleanArchitecture.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            // Register application services here
            services.AddScoped<ICategoryService, CategoryService>();
            return services;
        }
    }
}
