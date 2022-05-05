using System.Collections.Generic;
using System.Threading.Tasks;
using SwapExchange.Entity;

namespace SwapExchange.Service
{
    public interface ITokenQueryAndAssembleService
    {
        Task QueryTokenAndAssembleSwapInfosAsync(PairsList pairsList,
            QueryTokenInfo queryTokenInfo);

        Task<List<string>> PreferredSwapPathAsync(string tokenSymbol, Dictionary<string, List<string>> canSwapMap,
            Dictionary<string, Path> pathMap);

        Task<PairsList> QueryTokenPairsFromChain();

        Task HandleTokenInfoAndSwap();
    }
}