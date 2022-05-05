using System.Collections.Generic;
using System.Linq;
using Awaken.Contracts.Swap;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace SwapExchange.Entity
{
    public class PairsList : IMessage
    {
        public List<string> Pairs { get; set; }
        
        
        public void MergeFrom(CodedInputStream input)
        {
            Pairs = StringList.Parser.ParseFrom(input).Value.ToList();
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