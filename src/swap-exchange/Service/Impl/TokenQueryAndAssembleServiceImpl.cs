using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QuadraticVote.Application.Service.Extensions;
using SwapExchange.Constant;
using SwapExchange.Entity;
using SwapExchange.Options;
using Volo.Abp.DependencyInjection;

namespace SwapExchange.Service.Implemention
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ITokenQueryAndAssembleService))]
    public class TokenQueryAndAssembleServiceImpl : ITransientDependency, ITokenQueryAndAssembleService
    {
        private readonly ILogger<TokenQueryAndAssembleServiceImpl> _logger;
        private readonly TokenOptions _tokenOptions;
        private readonly IAElfClientService _aElfClientService;

        public TokenQueryAndAssembleServiceImpl(TokenOptions tokenOptions, IAElfClientService aElfClientService,
            ILogger<TokenQueryAndAssembleServiceImpl> logger)
        {
            _tokenOptions = tokenOptions;
            _aElfClientService = aElfClientService;
            _logger = logger;
        }

        /**
         * Query supports token list and forecase price，final assembly.
         */
        public async Task<PretreatmentTokenInfo> QueryTokenAndAssembleSwapInfosAsync()
        {
            var pathMap = new Dictionary<string, string[]>();

            var queryTokenInfo = await ConvertTokens();
            // Get pairs list.
            var pairsList = await QueryTokenPairsFromChain();
            if (pairsList.Pairs.Count <= 0)
            {
                _logger.LogError("Get token pairs error,terminate！");
            }

            // Make paireList into Map
            var tokenCanSwapMap = await DisassemblePairsListIntoMap(pairsList);

            foreach (var item in queryTokenInfo.Items)
            {
                // Handle path
                // await PreferredSwapPathAsync(new string[]
                // {
                //     item.Token0.Symbol, item.Token1.Symbol
                // }, tokenCanSwapMap,pathMap);
                //
                // if (path.Length <= 0)
                // {
                //     continue;
                // }

                // Handle token amounts
                Console.WriteLine("11");
            }

            return null;
        }


        /**
         * Disassemble pair list for Preferred swap path.
         * <param name="pairsList"></param>
         */
        private async Task<Dictionary<string, List<string>>> DisassemblePairsListIntoMap(PairsList pairsList)
        {
            var map = new Dictionary<string, List<string>>();
            var pairs = pairsList.Pairs;
            foreach (var pair in pairs)
            {
                var tokens = LpTokenHelper.ExtractTokensFromTokenPair(pair);
                var token0 = tokens[0];
                var token1 = tokens[1];
                var token0List = map[token0] ?? new List<string>();
                if (!token0List.Contains(token1))
                {
                    token0List.Add(token1);
                }

                var token1List = map[token1] ?? new List<string>();
                if (!token1List.Contains(token0))
                {
                    token1List.Add(token0);
                }
            }

            return map;
        }

        /**
         * Preferred appropriate swap path.
         */
        public async Task<string[]> PreferredSwapPathAsync(string tokenSymbol,
            Dictionary<string, List<string>> canSwapMap,
            Dictionary<string, List<string>> pathMap)
        {
            if (!tokenSymbol.IsNullOrEmpty())
            {
                var path = pathMap[tokenSymbol] ?? new List<string>();
                if (path.Count > 0)
                {
                    return path.ToArray();
                }

                // path.Add(_tokenOptions.TargetToken);
                var canSwap = canSwapMap[tokenSymbol];
                foreach (var largeToken in _tokenOptions.LargeCurrencyTokens)
                {
                    if (canSwap.Contains(largeToken))
                    {
                        path.AddFirst(largeToken);
                        path.AddFirst(tokenSymbol);
                    }
                    else
                    {
                        var list = new List<List<string>>();
                        await RecursionHandlePath(tokenSymbol, canSwapMap, path, list);
                        if (list.Count > 0)
                        {
                            Console.WriteLine(list);
                        }
                    }
                }
            }
            else
            {
                return new string[] { };
            }

            return null;
        }

        private async Task RecursionHandlePath(string token, Dictionary<string, List<string>> canSwapMap,
            List<string> path, List<List<string>> list)
        {
            var canSwapTokens = canSwapMap[token];
            foreach (var canSwapToken in canSwapTokens)
            {
                if (path.Count == 0)
                {
                    path.AddLast(token);
                    list.AddLast(path);
                }

                if (path[path.Count - 1].Equals(_tokenOptions.TargetToken))
                {
                    continue;
                }

                if (path.Contains(canSwapToken))
                {
                    continue;
                }

                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].SequenceEqual(path))
                    {
                        path.Add(canSwapToken);
                        list[i] = path;
                    }
                }

                if (!path[path.Count - 1].Equals(canSwapToken))
                {
                    path.Add(canSwapToken);
                    list.Add(path);
                }

                await RecursionHandlePath(canSwapToken, canSwapMap, path, list);
            }
        }

        /**
         * Query pairs list from chain.
         */
        public async Task<PairsList> QueryTokenPairsFromChain()
        {
            return await _aElfClientService.QueryAsync<PairsList>(_tokenOptions.SwapContractAddress,
                _tokenOptions.OperatorPrivateKey,
                ContractOperateConst.SWAP_PAIRS_LIST_METHOD, new Empty());
        }

        private async Task<QueryTokenInfo> ConvertTokens()
        {
            var queryStr = await QueryTokenList();
            return JsonConvert.DeserializeObject<QueryTokenInfo>(queryStr);
        }

        private async Task<string> QueryTokenList()
        {
            try
            {
                string response = null;
                string statusCode;
                do
                {
                    response = HttpClientHelper.GetResponse(_tokenOptions.QueryTokenUrl, out statusCode);
                } while (!statusCode.Equals("OK"));

                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return await QueryTokenList();
            }
        }
    }
}