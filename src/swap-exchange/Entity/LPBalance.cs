using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace SwapExchange.Entity
{
    public class LPBalance:IMessage
    {   
        
        public string Symbol { get; set; }
        public Address Owner { get; set; }
        public long Amount { get; set; }
        
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