using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace SwapExchange.Entity
{
    public class GetAmountOutInput:IMessage
    {   
        public List<string> Path { get; set; }
        public long AmountIn { get; set; }
        

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