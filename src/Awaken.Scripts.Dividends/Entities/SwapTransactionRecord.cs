using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Awaken.Scripts.Dividends.Entities
{
    public class SwapTransactionRecord : AuditedAggregateRoot<Guid>
    {
        public string TransactionId { get; set; }
    }
}