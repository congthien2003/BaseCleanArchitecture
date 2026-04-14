using System;
using System.Collections.Generic;
using System.Text;

namespace BaseCleanArchitecture.Application.Interfaces
{
    public interface ICacheService
    {
        void Cache(string key, string value);
    }
}
