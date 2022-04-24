using System;
using System.Linq;

namespace QuadraticVote.Application.Service.Extensions
{
    public class LpTokenHelper
    {
        public static string GetTokenPairSymbol(string tokenA, string tokenB)
        {
            var symbols = SortSymbols(tokenA, tokenB);
            return $"ALP {symbols[0]}-{symbols[1]}";
        }

        public static string[] SortSymbols(params string[] symbols)
        {
            return symbols.OrderBy(s => s).ToArray();
        }
        
        public static string ExtractTokenPairFromSymbol(string symbol)
        {
            // ReSharper disable once PossibleNullReferenceException
            return symbol.StartsWith("ALP")
                ? symbol.Substring(symbol.IndexOf("ALP", StringComparison.Ordinal) + "ALP".Length).Trim()
                : symbol.Trim();
        }

        public static string[] ExtractTokensFromTokenPair(string tokenPair)
        {   
            return SortSymbols(tokenPair.Split('-'));
        }
    }
}