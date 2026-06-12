using BaseCleanArchitecture.Application.Common.Models;

namespace BaseCleanArchitecture.Application.Abstractions.Authentication
{
    public interface ICurrentUserService
    {
        public CurrentUser CurrentUser { get; }
    }
}
