using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awaken.Contracts.Swap;
using Awaken.Contracts.SwapExchangeContract;
using Awaken.Contracts.Token;
using Awaken.Scripts.Dividends.Entities;
using Awaken.Scripts.Dividends.Extensions;
using Awaken.Scripts.Dividends.Helpers;
using Awaken.Scripts.Dividends.Options;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Org.BouncyCastle.Math;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;
using Token = Awaken.Contracts.SwapExchangeContract.Token;

namespace Awaken.Scripts.Dividends.Services
{
    public class TokenQueryAndAssembleService : ITransientDependency, ITokenQueryAndAssembleService
    {
        private readonly ILogger<TokenQueryAndAssembleService> _logger;
        private readonly DividendsScriptOptions _dividendsScriptOptions;
        private readonly IAElfClientService _clientService;
        private readonly IAutoObjectMappingProvider _mapper;
        private readonly IRepository<SwapTransactionRecord, Guid> _repository;

        public TokenQueryAndAssembleService(IOptionsSnapshot<DividendsScriptOptions> tokenOptions,
            IAElfClientService clientService,
            ILogger<TokenQueryAndAssembleService> logger, IAutoObjectMappingProvider mapper,
            IRepository<SwapTransactionRecord, Guid> repository)
        {
            _dividendsScriptOptions = tokenOptions.Value;
            _clientService = clientService;
            _logger = logger;
            _mapper = mapper;
            _repository = repository;
        }

        /// <summary>
        /// start
        /// </summary>
        public async Task HandleTokenInfoAndSwap()
        {
            int pageNum = 1;
            var pairsList = await QueryTokenPairsFromChain();
            var queryTokenInfo = await ConvertTokens(pageNum);
            var items = queryTokenInfo.Items;
            while (items.Count > 0)
            {
                var tmp = items.Take(_dividendsScriptOptions.BatchAmount).ToList();
                await QueryTokenAndAssembleSwapInfosAsync(pairsList, queryTokenInfo);
                tmp.RemoveRange(0, tmp.Count);
            }
        }

        /// <summary>
        /// Query supports token list and forecase price，final assembly.
        /// </summary>
        /// <param name="pairsList"></param>
        /// <param name="queryTokenInfo"></param>
        public async Task QueryTokenAndAssembleSwapInfosAsync(StringList pairsList,
            QueryTokenInfo queryTokenInfo)
        {
            // Swap path map. token-->path
            var pathMap = new Dictionary<string, Path>();
            var tokenList = new TokenList();
            if (pairsList.Value.Count <= 0)
            {
                _logger.LogError("Get token pairs error,terminate！");
            }

            // Make pairList into Map
            var tokenCanSwapMap = await DisassemblePairsListIntoMap(pairsList);

            foreach (var item in queryTokenInfo.Items)
            {
                // Handle path ，expect price,slip point percentage
                await HandleSwapPathAndTokenInfoAsync(new string[]
                {
                    item.Token0.Symbol, item.Token1.Symbol
                }, tokenCanSwapMap, pathMap, tokenList);
            }

            var swapTokensInput = new SwapTokensInput
            {
                PathMap = { pathMap },
                SwapTokenList = tokenList
            };

            var transactionId = await _clientService.SendTransactionAsync(
                _dividendsScriptOptions.SwapToolContractAddress,
                _dividendsScriptOptions.OperatorPrivateKey, ContractMethodNameConstants.SwapLpTokens, swapTokensInput);

            if (!string.IsNullOrEmpty(transactionId))
            {
                await _repository.InsertAsync(new SwapTransactionRecord
                {
                    TransactionId = transactionId
                });
            }
        }

        /// <summary>
        /// Disassemble pair list for Preferred swap path.
        /// </summary>
        /// <param name="pairsList"></param>
        /// <returns></returns>
        private async Task<Dictionary<string, List<string>>> DisassemblePairsListIntoMap(StringList pairsList)
        {
            var map = new Dictionary<string, List<string>>();
            var pairs = pairsList.Value;
            foreach (var pair in pairs)
            {
                var tokens = LpTokenHelper.ExtractTokensFromTokenPair(pair);
                var token0 = tokens[0];
                var token1 = tokens[1];
                var token0List = map.GetValueOrDefault(token0) ?? new List<string>();
                if (!token0List.Contains(token1))
                {
                    token0List.Add(token1);
                    map[token0] = token0List;
                }

                var token1List = map.GetValueOrDefault(token1) ?? new List<string>();
                if (!token1List.Contains(token0))
                {
                    token1List.Add(token0);
                    map[token1] = token1List;
                }
            }

            return map;
        }

        private async Task HandleSwapPathAndTokenInfoAsync(string[] tokens,
            Dictionary<string, List<string>> canSwapMap,
            Dictionary<string, Path> pathMap, TokenList tokenList)
        {
            // Get FeeTo lp token amount.
            var lpTokenSymbol = LpTokenHelper.GetTokenPairSymbol(tokens.First(), tokens.Last());
            var pair = LpTokenHelper.ExtractTokenPairFromSymbol(lpTokenSymbol);
            var address = await _clientService.GetAddressFromPrivateKey(_dividendsScriptOptions.OperatorPrivateKey);
            var balance = await _clientService.QueryAsync<Balance>(_dividendsScriptOptions.LpTokenContractAddresses,
                _dividendsScriptOptions.OperatorPrivateKey, ContractMethodNameConstants.GetBalance,
                new GetBalanceInput
                {
                    Owner = address.ToAddress(),
                    Symbol = lpTokenSymbol
                });
            if (balance.Amount <= 0)
            {
                return;
            }

            // approve
            await _clientService.SendTransactionAsync(_dividendsScriptOptions.LpTokenContractAddresses,
                _dividendsScriptOptions.OperatorPrivateKey, ContractMethodNameConstants.Approve, new ApproveInput
                {
                    Amount = balance.Amount,
                    Spender = _dividendsScriptOptions.SwapToolContractAddress.ToAddress(),
                    Symbol = lpTokenSymbol
                });

            // Get Reserves.
            var getReservesOutput = await _clientService.QueryAsync<GetReservesOutput>(
                _dividendsScriptOptions.SwapContractAddress,
                _dividendsScriptOptions.OperatorPrivateKey,
                ContractMethodNameConstants.GetReserves,
                new GetReservesInput
                {
                    SymbolPair = { pair }
                });

            // Get lpToken total supply.
            var getTotalSupplyOutput = await _clientService.QueryAsync<GetTotalSupplyOutput>(
                _dividendsScriptOptions.SwapContractAddress,
                _dividendsScriptOptions.OperatorPrivateKey,
                ContractMethodNameConstants.GetTotalSupply, new StringList
                {
                    Value = { pair }
                });

            // Amount of tokens could get from removed liquidity.
            var amountsExcept = await ComputeAmountFromRemovedLiquidity(balance.Amount, getReservesOutput.Results.First(),
                getTotalSupplyOutput.Results.First().TotalSupply);

            foreach (var token in tokens)
            {
                if (token == _dividendsScriptOptions.TargetToken)
                {
                    continue;
                }

                await PreferredSwapPathAsync(token, canSwapMap, pathMap);
                long amountIn = 0;
                amountIn = getReservesOutput.Results.First().SymbolA.Equals(token)
                    ? amountsExcept.First()
                    : amountsExcept.Last();

                await BudgetTokenExpectPrice(token, pathMap, amountIn);
            }

            // Add token amount 
            tokenList.TokensInfo.Add(new Token
            {
                Amount = balance.Amount,
                TokenSymbol = lpTokenSymbol
            });
        }

        private async Task<List<long>> ComputeAmountFromRemovedLiquidity(long liquidityRemoveAmount,
            ReservePairResult reserves, long totalSupply)
        {
            var result = new List<long>
            {
                new BigInteger(liquidityRemoveAmount.ToString())
                    .Multiply(new BigInteger(reserves.ReserveA.ToString()))
                    .Divide(new BigInteger(totalSupply.ToString()))
                    .LongValue,
                new BigInteger(liquidityRemoveAmount.ToString())
                    .Multiply(new BigInteger(reserves.ReserveB.ToString()))
                    .Divide(new BigInteger(totalSupply.ToString()))
                    .LongValue
            };
            return result;
        }

        private async Task BudgetTokenExpectPrice(string token, Dictionary<string, Path> pathMap,
            long amountIn)
        {
            var path = pathMap.GetValueOrDefault(token);
            if (path is { Value.Count: > 0 })
            {
                var expect = await _clientService.QueryAsync<GetAmountsOutOutput>(_dividendsScriptOptions.SwapContractAddress,
                    _dividendsScriptOptions.OperatorPrivateKey, ContractMethodNameConstants.GetAmountsOut,
                    new GetAmountsOutInput
                    {
                        Path = { pathMap[token].Value },
                        AmountIn = amountIn
                    });
                var targetTokenOut = expect.Amount.Last();
                var expectPrice = new BigInteger(DividendsScriptConstants.ExpansionCoefficient)
                    .Multiply(BigInteger.ValueOf(targetTokenOut))
                    .Divide(BigInteger.ValueOf(amountIn));
                // todo parameter type
                pathMap[token].ExpectPrice = expectPrice.ToString();
                pathMap[token].SlipPoint = _dividendsScriptOptions.SlipPointPercent;
            }
        }

        public async Task<List<string>> PreferredSwapPathAsync(string tokenSymbol,
            Dictionary<string, List<string>> canSwapMap,
            Dictionary<string, Path> pathMap)
        {
            if (!tokenSymbol.IsNullOrEmpty())
            {
                var path = pathMap.GetValueOrDefault(tokenSymbol);
                if (path != null)
                {
                    if (path.Value is { Count: > 0 })
                    {
                        return path.Value.ToList();
                    }
                }
                else
                {
                    pathMap[tokenSymbol] = new Path();
                }

                var pathList = new List<List<string>>();
                await RecursionHandlePath(tokenSymbol, canSwapMap, null, pathList);
                if (pathList.Count > 0)
                {
                    List<string> tmp = null;
                    foreach (var list in pathList)
                    {
                        if (tmp == null)
                        {
                            tmp = list;
                            continue;
                        }

                        if (tmp.Count > list.Count)
                        {
                            tmp = list;
                        }
                    }

                    pathMap[tokenSymbol] = new Path
                    {
                        Value = { tmp }
                    };
                    return tmp;
                }
            }

            return new List<string>();
        }

        private async Task RecursionHandlePath(string token, Dictionary<string, List<string>> canSwapMap,
            List<string> currentPath, List<List<string>> pathList)
        {
            var canSwapTokens = canSwapMap[token];
            foreach (var canSwapToken in canSwapTokens)
            {
                if (currentPath == null)
                {
                    currentPath = new List<string>();
                }

                if (currentPath.Count == 0)
                {
                    currentPath.AddFirst(token);
                }

                if (currentPath.Contains(canSwapToken))
                {
                    continue;
                }

                // deep copy
                var tmp = currentPath.Select(s => s).ToList();
                tmp.Add(canSwapToken);
                if (canSwapToken.Equals(_dividendsScriptOptions.TargetToken))
                {
                    pathList.Add(tmp);
                }
                else
                {
                    await RecursionHandlePath(canSwapToken, canSwapMap, tmp, pathList);
                }
            }
        }

        public async Task<StringList> QueryTokenPairsFromChain()
        {
            return await _clientService.QueryAsync<StringList>(_dividendsScriptOptions.SwapContractAddress,
                _dividendsScriptOptions.OperatorPrivateKey,
                ContractMethodNameConstants.GetPairs, new Empty());
        }

        private async Task<QueryTokenInfo> ConvertTokens(int pageNum)
        {
            var queryStr = await QueryTokenList(pageNum);
            return JsonConvert.DeserializeObject<QueryTokenInfo>(queryStr);
        }

        private async Task<string> QueryTokenList(int pageNum)
        {
            const int maxResultCount = 999;
            const int skipCount = 0;
            try
            {
                string response;
                string statusCode;
                do
                {
                    // response = HttpClientHelper.GetResponse(string.Format(_dividendsScriptOptions.QueryTokenUrl,maxResultCount,skipCount,_dividendsScriptOptions.FeeRate), out statusCode);
                    response = HttpClientHelper.GetResponse(
                        string.Format(_dividendsScriptOptions.QueryTokenUrl, maxResultCount, skipCount, _dividendsScriptOptions.FeeRate),
                        out statusCode);
                } while (!statusCode.Equals("OK"));

                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return await QueryTokenList(pageNum);
            }
        }
    }
}