using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Services;
using BaseCleanArchitecture.Domain.Services;
using BaseCleanArchitecture.Domain.Services.Implementations;
using BaseCleanArchitecture.Domain.Services.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BaseCleanArchitecture.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Register Domain Service
        services.AddDomainServices();

        // Register application services here
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Register domain services here
        services.AddScoped<IBaseDomainService, BaseDomainService>();
        return services;
    }
}
