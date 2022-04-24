using System.Collections.Generic;
using System.Threading.Tasks;
using SwapExchange.Entity;

namespace SwapExchange.Service
{
    public interface ITokenQueryAndAssembleService
    {
        Task<PretreatmentTokenInfo> QueryTokenAndAssembleSwapInfosAsync();

        Task<string[]> PreferredSwapPathAsync(string tokenSymbol, Dictionary<string, List<string>> canSwapMap,
            Dictionary<string, List<string>> pathMap);

        Task<PairsList> QueryTokenPairsFromChain();
    }
}