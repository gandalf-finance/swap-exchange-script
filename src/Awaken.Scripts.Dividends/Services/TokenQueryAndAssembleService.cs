using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Google.Protobuf;
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
        private readonly IDividendService _dividendService;

        public TokenQueryAndAssembleService(IOptionsSnapshot<DividendsScriptOptions> tokenOptions,
            IAElfClientService clientService,
            ILogger<TokenQueryAndAssembleService> logger,
            IAElfTokenService elfTokenService, IDividendService dividendService)
        {
            _dividendsScriptOptions = tokenOptions.Value;
            _clientService = clientService;
            _logger = logger;
            _elfTokenService = elfTokenService;
            _dividendService = dividendService;
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
            while (items.Count > 0)
            {
                var takeAmount = Math.Min(_dividendsScriptOptions.BatchAmount, items.Count);
                var handleItems = items.Take(takeAmount).ToList();
                await QueryTokenAndAssembleSwapInfosAsync(tokenSwapMap, handleItems);
                items.RemoveRange(0, takeAmount);
            }

            if (isNewReward)
            {
                await NewRewardAsync(_dividendsScriptOptions.TargetToken);
            }
        }

        public async Task QueryTokenAndAssembleSwapInfosAsync(Dictionary<string, List<string>> tokenCanSwapMap,
            List<Item> handleItems)
        {
            // Swap path map. token-->path
            var pathMap = new Dictionary<string, Path>();
            var tokenList = new TokenList();

            foreach (var item in handleItems)
            {
                try
                {
                    await HandleSwapPathAndTokenInfoAsync(new string[]
                    {
                        item.Token0.Symbol, item.Token1.Symbol
                    }, tokenCanSwapMap, pathMap, tokenList);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception.Message);
                    _logger.LogError(exception.StackTrace);
                }
                // Handle path ，expect price,slip point percentage
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

            await _clientService.SendTransactionAsync(
                _dividendsScriptOptions.SwapToolContractAddress,
                _dividendsScriptOptions.OperatorPrivateKey, ContractMethodNameConstants.SwapLpTokens, swapTokensInput);
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

        private async Task HandleSwapPathAndTokenInfoAsync(string[] tokens,
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
                return;
            }
            
            foreach (var token in tokens)
            {
                if (token == _dividendsScriptOptions.TargetToken)
                {
                    continue;
                }

                if (await PreferredSwapPathAsync(token, canSwapMap, pathMap) != null) continue;
                _logger.LogInformation($"Skip LP token:{lpTokenSymbol} because it can't find path for {token}");
                return;
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

                await BudgetTokenExpectPriceAsync(token, pathMap, amountIn);
                await ApproveToSwapExchangeAsync(_dividendsScriptOptions.OperatorPrivateKey, address, spender, token);
            }

            // Add token amount 
            tokenList.TokensInfo.Add(new Token
            {
                Amount = balance.Amount,
                TokenSymbol = lpTokenSymbol
            });
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
            try
            {
                string response;
                string statusCode;
                do
                {
                    _logger.LogInformation("Start querying token");
                    // response = HttpClientHelper.GetResponse(string.Format(_dividendsScriptOptions.QueryTokenUrl,maxResultCount,skipCount,_dividendsScriptOptions.FeeRate), out statusCode);
                    response = HttpClientHelper.GetResponse(
                        string.Format(_dividendsScriptOptions.QueryTokenUrl, maxResultCount, skipCount,
                            _dividendsScriptOptions.FeeRate),
                        out statusCode);
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

        private async Task NewRewardAsync(string targetToken)
        {
            var operatorKey = _dividendsScriptOptions.ReceiverPrivateKey;
            if (operatorKey.IsNullOrEmpty())
            {
                throw new Exception("Failed to send NewReward transactions because of lacking of Receiver private key");
            }

            var userAddress =
                (await _clientService.GetAddressFromPrivateKey(operatorKey)).ToAddress();
            var address = GetAddress(userAddress);
            var dividendAddressBase58Str = _dividendsScriptOptions.DividendContractAddresses;
            var dividendAddress = GetAddress(dividendAddressBase58Str.ToAddress());

            var balance = await ApproveAllTokenBalanceAsync(operatorKey, address, dividendAddress,
                dividendAddressBase58Str, targetToken);
            balance -= balance / 5;
            if (balance <= 0)
            {
                return;
            }

            // new reward
            await _dividendService.NewRewardAsync(operatorKey, targetToken, balance);
        }

        private async Task ApproveToSwapExchangeAsync(string operatorKey, AElf.Types.Address address,
            AElf.Types.Address spender, string token)
        {
            var owner = GetAddress(address);
            var swapExchangeBase58Address = _dividendsScriptOptions.SwapToolContractAddress;
            var swapExchangeAddress = GetAddress(spender);
            await ApproveAllTokenBalanceAsync(operatorKey, owner, swapExchangeAddress, swapExchangeBase58Address,
                token);
        }

        private async Task<long> ApproveAllTokenBalanceAsync(string operatorKey, AElf.Client.Proto.Address owner,
            AElf.Client.Proto.Address spender, string spenderBase58, string targetToken)
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
    }
}