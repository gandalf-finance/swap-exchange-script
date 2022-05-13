using System;
using System.Collections.Generic;

namespace Awaken.Scripts.Dividends.Entities
{
    public class QueryTokenInfo
    {
        public int TotalCount { get; set; }
        public List<Item> Items { get; set; }
    }


    public class Item
    {   
        public string Id { get; set; } 
        public bool IsTokenReversed { get; set; }
        public string ChainId { get; set; }
        public Token Token0 { get; set; }
        public Token Token1 { get; set; }
        
        public string FeeRate { get; set; }
    }

    public class Token
    {
        public string Address { get; set; }
        public string Symbol { get; set; }
        public int Decimals { get; set; }
    }
}