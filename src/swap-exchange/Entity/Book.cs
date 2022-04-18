using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SwapExchange.Entity
{
    public class Book : AuditedAggregateRoot<Guid>
    {   
        public string Name { get; set; }
        public string Price { get; set; }
    }
}