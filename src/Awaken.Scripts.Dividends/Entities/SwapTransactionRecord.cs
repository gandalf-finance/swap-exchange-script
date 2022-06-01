using System;
using Awaken.Scripts.Dividends.Enum;
using Volo.Abp.Domain.Entities.Auditing;

namespace Awaken.Scripts.Dividends.Entities
{
    public class SwapTransactionRecord : AuditedAggregateRoot<Guid>
    {
        public string TransactionId { get; set; }
        public TransactionStatus TransactionStatus { get; set; }
        public TransactionType TransactionType { get; set; }

        public SwapTransactionRecord()
        {
            TransactionStatus = TransactionStatus.NotChecked;
        }
    }
}