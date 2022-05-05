using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace SwapExchange.Entity
{
    public class GetReservesOutput:IMessage
    {   
        
        public List<ReservePairResult> Results { get; set; }

        public void MergeFrom(CodedInputStream input)
        {
            throw new System.NotImplementedException();
        }

        public void WriteTo(CodedOutputStream output)
        {
            throw new System.NotImplementedException();
        }

        public int CalculateSize()
        {
            throw new System.NotImplementedException();
        }

        public MessageDescriptor Descriptor { get; }
        
        public class ReservePairResult
        {
            public string SymbolPair { get; set; }
            public string SymbolA { get; set; }
            public string SymbolB { get; set; }
            public long ReserveA { get; set; }
            public long ReserveB { get; set; }
            public long BlockTimestampLast { get; set; }
        }
    }
    
}