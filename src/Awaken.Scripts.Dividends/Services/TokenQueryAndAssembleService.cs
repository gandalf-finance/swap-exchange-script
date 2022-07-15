using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.Internal;
using Awaken.Contracts.Swap;
using Awaken.Contracts.SwapExchangeContract;
using Awaken.Contracts.Token;
using Awaken.Scripts.Dividends.Entities;
using Awaken.Scripts.Dividends.Extensions;
using Awaken.Scripts.Dividends.Handlers.Events;
using Awaken.Scripts.Dividends.Helpers;
using Awaken.Scripts.Dividends.Options;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Org.BouncyCastle.Math;
using Volo.Abp.DependencyInjection;
using Google.Protobuf;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Guids;
using GetBalanceInput = Awaken.Contracts.Token.GetBalanceInput;
using Token = Awaken.Contracts.SwapExchangeContract.Token;
using TokenList = Awaken.Contracts.SwapExchangeContract.TokenList;

namespace Awaken.Scripts.Dividends.Services
{
    public class TokenQueryAndAssembleService : ITransientDependency, ITokenQueryAndAssembleService
    {
        private readonly ILogger<TokenQueryAndAssembleService> _logger;
        private readonly DividendsScriptOptions _dividendsScriptOptions;
        private readonly IAElfClientService _clientService;
        private readonly IAElfTokenService _elfTokenService;
        private readonly ILocalEventBus _localEventBus;
        private readonly IGuidGenerator _generator;

        public TokenQueryAndAssembleService(IOptionsSnapshot<DividendsScriptOptions> tokenOptions,
            IAElfClientService clientService,
            ILogger<TokenQueryAndAssembleService> logger,
            IAElfTokenService elfTokenService, ILocalEventBus localEventBus,
            IGuidGenerator generator)
        {
            _dividendsScriptOptions = tokenOptions.Value;
            _clientService = clientService;
            _logger = logger;
            _elfTokenService = elfTokenService;
            _localEventBus = localEventBus;
            _generator = generator;
        }

        /// <summary>
        /// start
        /// </summary>
        public async Task HandleTokenInfoAndSwap(bool isNewReward)
        {
            var pairList = await QueryTokenPairsFromChain();
            _logger.LogInformation($"pair list count {pairList.Value.Count}");
            if (pairList.Value.Count <= 0)
            {
                return;
            }

            var queryTokenInfo = await ConvertTokensAsync();
            var items = queryTokenInfo.Items;
            var tokenSwapMap = DisassemblePairsListIntoMap(pairList);
            CheckTokenItems(items, tokenSwapMap);
            Guid? newId = isNewReward? _generator.Create() : null;
            while (items.Count > 0)
            {
                var takeAmount = Math.Min(_dividendsScriptOptions.BatchAmount, items.Count);
                var handleItems = items.Take(takeAmount).ToList();
                await QueryTokenAndAssembleSwapInfosAsync(tokenSwapMap, handleItems, newId);
                items.RemoveRange(0, takeAmount);
            }
        }

        private async Task QueryTokenAndAssembleSwapInfosAsync(Dictionary<string, List<string>> tokenCanSwapMap,
            List<Item> handleItems, Guid? newId)
        {
            // Swap path map. token-->path
            var pathMap = new Dictionary<string, Path>();
            var tokenList = new TokenList();

            foreach (var tokens in handleItems.Select(item => new []
                     {
                         item.Token0.Symbol, item.Token1.Symbol
                     }))
            {
                try
                {
                    if (
                        !await HandleSwapPathAndTokenInfoAsync(tokens, tokenCanSwapMap, pathMap, tokenList))
                    {
                        FixPathMap(tokenList, tokens);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception.Message);
                    _logger.LogError(exception.StackTrace);
                    FixPathMap(tokenList, tokens);
                }
            }

            if (tokenList.TokensInfo.Count == 0)
            {
                _logger.LogInformation("0 token in QueryTokenAndAssembleSwapInfosAsync");
                return;
            }
            
            var swapTokensInput = new SwapTokensInput
            {
                PathMap = { pathMap },
                SwapTokenList = tokenList
            };
            var txId = await _clientService.SendTransactionAsync(
                _dividendsScriptOptions.SwapToolContractAddress,
                _dividendsScriptOptions.OperatorPrivateKey, ContractMethodNameConstants.SwapLpTokens, swapTokensInput);

            if (!newId.HasValue)
            {
                return;
            }

            await _localEventBus.PublishAsync(new ToSwapTokenEvent
            {
                Id = newId.Value,
                TransactionId = txId
            });
        }

        /// <summary>
        /// Disassemble pair list for Preferred swap path.
        /// </summary>
        /// <param name="pairsList"></param>
        /// <returns></returns>
        private Dictionary<string, List<string>> DisassemblePairsListIntoMap(StringList pairsList)
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

        private async Task<bool> HandleSwapPathAndTokenInfoAsync(string[] tokens,
            Dictionary<string, List<string>> canSwapMap,
            Dictionary<string, Path> pathMap, TokenList tokenList)
        {
            // Get FeeTo lp token amount.
            var lpTokenSymbol = LpTokenHelper.GetTokenPairSymbol(tokens.First(), tokens.Last());
            var pair = LpTokenHelper.ExtractTokenPairFromSymbol(lpTokenSymbol);
            // Get Reserves.
            var getReservesOutput = await _clientService.QueryAsync<GetReservesOutput>(
                _dividendsScriptOptions.SwapContractAddress,
                _dividendsScriptOptions.OperatorPrivateKey,
                ContractMethodNameConstants.GetReserves,
                new GetReservesInput
                {
                    SymbolPair = { pair }
                });
            var address = (await _clientService.GetAddressFromPrivateKey(_dividendsScriptOptions.OperatorPrivateKey))
                .ToAddress();
            var balance = await _clientService.QueryAsync<Balance>(_dividendsScriptOptions.LpTokenContractAddresses,
                _dividendsScriptOptions.OperatorPrivateKey, ContractMethodNameConstants.GetBalance,
                new GetBalanceInput
                {
                    Owner = address,
                    Symbol = lpTokenSymbol
                });
            if (balance.Amount <= 0)
            {
                _logger.LogInformation($"Lp Token: {lpTokenSymbol} Balance is zero");
                return false;
            }

            foreach (var token in tokens)
            {
                if (token == _dividendsScriptOptions.TargetToken)
                {
                    continue;
                }

                if (await PreferredSwapPathAsync(token, canSwapMap, pathMap) != null) continue;
                _logger.LogInformation($"Skip LP token:{lpTokenSymbol} because it can't find path for {token}");
                return false;
            }

            // approve
            var spender = _dividendsScriptOptions.SwapToolContractAddress.ToAddress();
            var toApproveAmount = balance.Amount;
            if (toApproveAmount > 0)
            {
                var txId = await _clientService.SendTransactionAsync(_dividendsScriptOptions.LpTokenContractAddresses,
                    _dividendsScriptOptions.OperatorPrivateKey, ContractMethodNameConstants.Approve, new ApproveInput
                    {
                        Amount = toApproveAmount,
                        Spender = spender,
                        Symbol = lpTokenSymbol
                    });
                _logger.LogInformation(
                    $"Approve token: {lpTokenSymbol}, amount: {toApproveAmount} to {spender}\n Transaction Id: {txId}");
            }

            // Get lpToken total supply.
            var getTotalSupplyOutput = await _clientService.QueryAsync<GetTotalSupplyOutput>(
                _dividendsScriptOptions.SwapContractAddress,
                _dividendsScriptOptions.OperatorPrivateKey,
                ContractMethodNameConstants.GetTotalSupply, new StringList
                {
                    Value = { pair }
                });

            // Amount of tokens could get from removed liquidity.
            var amountsExcept = ComputeAmountFromRemovedLiquidity(balance.Amount,
                getReservesOutput.Results.First(),
                getTotalSupplyOutput.Results.First().TotalSupply);

            foreach (var token in tokens)
            {
                var amountIn = getReservesOutput.Results.First().SymbolA.Equals(token)
                    ? amountsExcept.First()
                    : amountsExcept.Last();

                try
                {
                    await BudgetTokenExpectPriceAsync(token, pathMap, amountIn);
                }
                catch (Exception exception)
                {
                    var pathInfo = new StringBuilder();
                    var path = pathMap[token];
                    path.Value.ForAll(p => pathInfo.Append(p + "=>"));
                    pathInfo.Remove(pathInfo.Length - 2, 2);
                    _logger.LogWarning($"Failed swap token: {token}, path: {pathInfo}");
                    _logger.LogWarning(exception.Message);
                    return false;
                }

                await ApproveToSwapExchangeAsync(_dividendsScriptOptions.OperatorPrivateKey, address, token);
            }

            // Add token amount 
            tokenList.TokensInfo.Add(new Token
            {
                Amount = balance.Amount,
                TokenSymbol = lpTokenSymbol
            });
            return true;
        }

        private List<long> ComputeAmountFromRemovedLiquidity(long liquidityRemoveAmount,
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

        private async Task BudgetTokenExpectPriceAsync(string token, Dictionary<string, Path> pathMap,
            long amountIn)
        {
            var path = pathMap.GetValueOrDefault(token);
            if (path is { Value.Count: > 0 })
            {
                var expect = await _clientService.QueryAsync<GetAmountsOutOutput>(
                    _dividendsScriptOptions.SwapContractAddress,
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
            if (tokenSymbol.IsNullOrEmpty()) return null;
            if (pathMap.TryGetValue(tokenSymbol, out var path))
            {
                return path.Value.ToList();
            }

            var pathList = new List<List<string>>();
            await RecursionHandlePath(tokenSymbol, canSwapMap, null, pathList);
            if (pathList.Count <= 0) return null;
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

        private async Task<QueryTokenInfo> ConvertTokensAsync()
        {
            var queryStr = await QueryTokenListAsync();
            return JsonConvert.DeserializeObject<QueryTokenInfo>(queryStr);
        }

        private async Task<string> QueryTokenListAsync()
        {
            const int maxResultCount = 999;
            const int skipCount = 0;
            var url = string.Format(_dividendsScriptOptions.QueryTokenUrl, maxResultCount, skipCount,
                _dividendsScriptOptions.FeeRate);
            _logger.LogInformation($"Query url: {url}");
            try
            {
                string response;
                string statusCode;
                do
                {
                    _logger.LogInformation("Start querying token");
                    response = HttpClientHelper.GetResponse(url, out statusCode);
                } while (!statusCode.Equals("OK"));

                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return await QueryTokenListAsync();
            }
        }

        private void CheckTokenItems(IEnumerable<Item> items, Dictionary<string, List<string>> tokenSwapMap)
        {
            var itemCheckMsg = new StringBuilder();
            foreach (var item in items)
            {
                if (item.Token0.Symbol.IsNullOrEmpty() || item.Token1.Symbol.IsNullOrEmpty())
                {
                    itemCheckMsg.Append($"Lack of item token info: {item.Id}\n");
                }

                if (!EnsureTokenPairMapExisted(item.Token0.Symbol, tokenSwapMap))
                {
                    itemCheckMsg.Append($"Lack of token:{item.Token0.Symbol} swap map\n");
                }

                if (
                    !EnsureTokenPairMapExisted(item.Token1.Symbol, tokenSwapMap))
                {
                    itemCheckMsg.Append($"Lack of token:{item.Token1.Symbol} swap map\n");
                }
            }

            if (itemCheckMsg.Length > 0)
            {
                throw new Exception(itemCheckMsg.ToString());
            }
        }

        private bool EnsureTokenPairMapExisted(string tokenSymbol, Dictionary<string, List<string>> swapMap)
        {
            return tokenSymbol == _dividendsScriptOptions.TargetToken || swapMap.ContainsKey(tokenSymbol);
        }

        private async Task ApproveToSwapExchangeAsync(string operatorKey, AElf.Types.Address address, string token)
        {
            var owner = GetAddress(address);
            var swapExchangeBase58Address = _dividendsScriptOptions.SwapToolContractAddress;
            await ApproveAllTokenBalanceAsync(operatorKey, owner, swapExchangeBase58Address,
                token);
        }

        private async Task<long> ApproveAllTokenBalanceAsync(string operatorKey, AElf.Client.Proto.Address owner, string spenderBase58, string targetToken)
        {
            var balance = await _elfTokenService.GetBalanceAsync(operatorKey, owner, targetToken);
            _logger.LogInformation($"Token: {targetToken} Balance is {balance}");
            if (balance <= 0)
            {
                return balance;
            }

            // approve
            var toApproveAmount = balance;
            _logger.LogInformation($"Token: {targetToken} to approve: {toApproveAmount}");
            await _elfTokenService.ApproveTokenAsync(operatorKey, spenderBase58, toApproveAmount,
                targetToken);

            return balance;
        }

        private AElf.Client.Proto.Address GetAddress(AElf.Types.Address address)
        {
            var addressByteString = address.ToByteString();
            return AElf.Client.Proto.Address.Parser.ParseFrom(addressByteString);
        }

        private void FixPathMap(
            TokenList tokenList, string[] tokens)
        {
            var lpTokenSymbol = LpTokenHelper.GetTokenPairSymbol(tokens.First(), tokens.Last());
            tokenList.TokensInfo.RemoveAll(x => x.TokenSymbol == lpTokenSymbol);
        }
    }
}