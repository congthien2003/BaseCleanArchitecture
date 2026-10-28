using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BaseCleanArchitecture.Persistence.Repositories;

public sealed class UserRepository : RepositoryBase<User, Guid>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    private IQueryable<User> UsersWithRoles() => _context.Set<User>()
        .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission);

    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.ToLower();
        return await UsersWithRoles()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized, cancellationToken);
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.ToLower();
        return await UsersWithRoles()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, cancellationToken);
    }

    public async Task<User?> FindByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await UsersWithRoles().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.ToLower();
        return await _context.Set<User>().AnyAsync(u => u.Email.ToLower() == normalized, cancellationToken);
    }

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.ToLower();
        return await _context.Set<User>().AnyAsync(u => u.Username.ToLower() == normalized, cancellationToken);
    }
}
