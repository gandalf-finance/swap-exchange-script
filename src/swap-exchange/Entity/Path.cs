using System.Collections.Generic;

namespace SwapExchange.Entity
{
    public class Path
    {
        public List<string> Value { get; set; }
        public string ExpectPrice { get; set; }
        public long SlipPoint { get; set; }
    }
}