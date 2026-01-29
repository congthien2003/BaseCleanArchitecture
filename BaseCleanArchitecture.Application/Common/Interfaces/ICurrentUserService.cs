using BaseCleanArchitecture.Application.Common.Models;

namespace BaseCleanArchitecture.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        public CurrentUser CurrentUser { get; }
    }
}
