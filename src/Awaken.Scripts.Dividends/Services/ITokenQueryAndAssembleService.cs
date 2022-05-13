using System.Collections.Generic;
using System.Threading.Tasks;
using Awaken.Contracts.Swap;
using Awaken.Contracts.SwapExchangeContract;
using Awaken.Scripts.Dividends.Entities;

namespace Awaken.Scripts.Dividends.Services
{
    public interface ITokenQueryAndAssembleService
    {
        Task QueryTokenAndAssembleSwapInfosAsync(StringList pairsList,
            QueryTokenInfo queryTokenInfo);

        Task<List<string>> PreferredSwapPathAsync(string tokenSymbol, Dictionary<string, List<string>> canSwapMap,
            Dictionary<string, Path> pathMap);

        Task<StringList> QueryTokenPairsFromChain();

        Task HandleTokenInfoAndSwap();
    }
}