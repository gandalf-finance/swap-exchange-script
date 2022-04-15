using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SwapExchange.Entity
{
    public class Book : AuditedAggregateRoot<Guid>
    {   
        private string Name { get; set; }
        private long Price { get; set; }
    }
}