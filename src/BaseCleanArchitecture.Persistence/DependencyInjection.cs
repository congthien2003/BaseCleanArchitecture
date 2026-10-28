using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaseCleanArchitecture.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddTransient<IUnitOfWork, UnitOfWork>();

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetOtpRepository, PasswordResetOtpRepository>();
        services.AddScoped<IRepositoryBase<Role, Guid>, RepositoryBase<Role, Guid>>();
        services.AddScoped<IRepositoryBase<UserRole, Guid>, RepositoryBase<UserRole, Guid>>();
        services.AddScoped<IPermissionSeeder, PermissionSeeder>();

        var adminSeed = configuration.GetSection(AdminSeedOptions.SectionName);
        services.AddSingleton(new AdminSeedOptions(
            adminSeed["Email"], adminSeed["Password"], adminSeed["Username"], adminSeed["FullName"]));
        services.AddScoped<IAdminSeeder, AdminSeeder>();
        services.AddScoped<IWebhookLogsRepository, WebhookLogsRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        return services;
    }
}
