using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SwapExchange.Entity
{
    public class SwapTranscationRecord:AuditedAggregateRoot<Guid>
    {
        private string Txid { get; set; }

        public SwapTranscationRecord(string txid)
        {
            Txid = txid;
        }
    }
}