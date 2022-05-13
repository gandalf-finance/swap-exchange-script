using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Awaken.Scripts.Dividends.Entities
{
    public class SwapResult : AuditedAggregateRoot<Guid>
    {
        public string Symbol { get; set; }
        public bool Result { get; set; }
        public long Amount { get; set; }
        public bool IsLptoken { get; set; }
    }
}