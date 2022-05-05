using System.Collections.Generic;
using Google.Protobuf;

namespace SwapExchange.Entity
{
    public class GetTotalSupplyOutput:BaseMessage
    {
        public List<TotalSupplyResult> Results { get; set; }

        public class TotalSupplyResult
        {
            public string SymbolPair { get; set; }
            public long TotalSupply { get; set; }
        }
    }
    
}