using System.Collections.Generic;
using System.Linq;
using Awaken.Contracts.Swap;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace SwapExchange.Entity
{
    public class GetAmountOutOutput:IMessage
    {   
        
        public List<long> amount { get; set; }

        public void MergeFrom(CodedInputStream input)
        {
            amount = GetAmountsOutOutput.Parser.ParseFrom(input).Amount.ToList();
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