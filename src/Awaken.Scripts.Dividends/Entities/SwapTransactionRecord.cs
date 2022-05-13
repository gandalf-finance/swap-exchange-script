using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Awaken.Scripts.Dividends.Entities
{
    public class SwapTransactionRecord : AuditedAggregateRoot<Guid>
    {
        private string TxId { get; set; }

        public SwapTransactionRecord(string txId)
        {
            TxId = txId;
        }
    }
}