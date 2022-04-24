using System.Collections.Generic;

namespace SwapExchange.Options
{
    public class TokenOptions
    {   
        
        public string OperatorPrivateKey { get; set; }
        
        public string QueryTokenUrl { get; set; }
        
        public int SlipPointPercent { get; set; }
        
        public string SwapContractAddress { get; set; }
        
        public string SwapToolContractAddress { get; set; }
        
        public List<string> LpTokenContractAddresses { get; set; }
        
        public string TargetToken { get; set; }
        
        public List<string> LargeCurrencyTokens { get; set; }
    }
}