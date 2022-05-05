using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace SwapExchange.Entity
{
    public class GetReservesInput:IMessage
    {   
        public List<string> SymbolPair { get; set; }

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
    }
}