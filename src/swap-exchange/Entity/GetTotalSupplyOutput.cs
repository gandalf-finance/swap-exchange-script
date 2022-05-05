using System.Collections.Generic;
using System.Linq;
using Awaken.Contracts.Swap;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace SwapExchange.Entity
{
    public class GetTotalSupplyOutput:IMessage
    {
        public List<TotalSupplyResult> Results { get; set; }

        public void MergeFrom(CodedInputStream input)
        {
            Results = Awaken.Contracts.Swap.GetTotalSupplyOutput.Parser.ParseFrom(input).Results.ToList();
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