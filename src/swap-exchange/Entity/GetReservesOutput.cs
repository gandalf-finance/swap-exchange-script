using System.Collections.Generic;
using System.Linq;
using Awaken.Contracts.Swap;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace SwapExchange.Entity
{
    public class GetReservesOutput:IMessage
    {   
        public List<ReservePairResult> Results { get; set; }

        public void MergeFrom(CodedInputStream input)
        {
            Results = Awaken.Contracts.Swap.GetReservesOutput.Parser.ParseFrom(input).Results.ToList();
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
      
    }
    
}