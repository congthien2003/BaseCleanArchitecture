using BaseCleanArchitecture.Application.Common.Interfaces;
using BaseCleanArchitecture.Application.Common.Models;


namespace BaseCleanArchitecture.Persistence
{
    public class CurrentUserService : ICurrentUserService
    {
        public CurrentUser CurrentUser => new CurrentUser();
    }
}
