using System.Collections.Generic;

namespace SwapExchange.Entity
{
    public class SwapTokensInput:BaseMessage
    {
        public Dictionary<string,Path> PathMap { get; set; }
        public TokenList SwapTokenList { get; set; }
        
        public class TokenList
        {
            public List<Token> TokensInfo { get; set; }
        }
        
        public class Token
        {
            public string TokenSymbol { get; set; }
            public long Amount { get; set; }
        }
    }
    
}