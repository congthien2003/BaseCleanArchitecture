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
        services.AddScoped<IRepositoryBase<Role, Guid>, RepositoryBase<Role, Guid>>();
        services.AddScoped<IRepositoryBase<UserRole, Guid>, RepositoryBase<UserRole, Guid>>();

        return services;
    }
}
