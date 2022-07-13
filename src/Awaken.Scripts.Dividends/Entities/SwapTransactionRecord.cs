using System;
using Awaken.Scripts.Dividends.Enum;
using Volo.Abp.Domain.Entities.Auditing;

namespace Awaken.Scripts.Dividends.Entities
{
    public class SwapTransactionRecord : AuditedAggregateRoot<Guid>
    {
        public string TransactionId { get; set; }
        public TransactionStatus TransactionStatus { get; set; }
        public string MethodName { get; set; }
        public string ToAddress { get; set; }

        public SwapTransactionRecord()
        {
            TransactionStatus = TransactionStatus.NotChecked;
        }
    }
}