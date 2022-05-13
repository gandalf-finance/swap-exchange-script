using System;
using System.Linq;

namespace Awaken.Scripts.Dividends.Helpers;

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
        return symbol.StartsWith("ALP")
            ? symbol[(symbol.IndexOf("ALP", StringComparison.Ordinal) + "ALP".Length)..].Trim()
            : symbol.Trim();
    }

    public static string[] ExtractTokensFromTokenPair(string tokenPair)
    {
        return SortSymbols(tokenPair.Split('-'));
    }
}