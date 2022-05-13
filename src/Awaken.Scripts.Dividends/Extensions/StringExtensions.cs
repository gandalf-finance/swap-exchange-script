using AElf.Types;

namespace Awaken.Scripts.Dividends.Extensions;

public static class StringExtensions
{
    public static Address ToAddress(this string address)
    {
        return Address.FromBase58(address);
    }
}