using BaseCleanArchitecture.Application.Features.Auth.Models;
using BaseCleanArchitecture.Domain.Entities;

namespace BaseCleanArchitecture.Application.Abstractions.Authentication;

public interface IAuthService
{
    Task<User> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<User> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}
