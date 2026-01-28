using System;
using System.Collections.Generic;
using System.Text;

namespace BaseCleanArchitecture.Domain.Abtractions.Entities
{
    public interface IEntityAuditBase<Tkey> : IEntityBase<Tkey>, IAuditable
    {

    }
}
