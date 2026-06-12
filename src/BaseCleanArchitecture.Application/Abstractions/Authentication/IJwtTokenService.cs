using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Domain.Entities;

namespace BaseCleanArchitecture.Application.Abstractions.Authentication
{
    public interface IJwtTokenService
    {
        public AuthTokenDto GenerateToken(User user);
    }
}
