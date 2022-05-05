using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awaken.Contracts.Swap;
using Awaken.Contracts.SwapExchangeContract;
using Awaken.Contracts.Token;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Org.BouncyCastle.Math;
using QuadraticVote.Application.Service.Extensions;
using SwapExchange.Constant;
using SwapExchange.Entity;
using SwapExchange.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;
using GetReservesOutput = SwapExchange.Entity.GetReservesOutput;
using GetTotalSupplyOutput = SwapExchange.Entity.GetTotalSupplyOutput;
using Path = SwapExchange.Entity.Path;
using Token = Awaken.Contracts.SwapExchangeContract.Token;

namespace SwapExchange.Service.Implemention
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ITokenQueryAndAssembleService))]
    public class TokenQueryAndAssembleServiceImpl : ITransientDependency, ITokenQueryAndAssembleService
    {
        private readonly ILogger<TokenQueryAndAssembleServiceImpl> _logger;
        private readonly TokenOptions _tokenOptions;
        private readonly IAElfClientService _aElfClientService;
        private readonly IAutoObjectMappingProvider _mapper;
        private readonly IRepository<SwapResult, Guid> _repository;

        public TokenQueryAndAssembleServiceImpl(IOptionsSnapshot<TokenOptions> tokenOptions,
            IAElfClientService aElfClientService,
            ILogger<TokenQueryAndAssembleServiceImpl> logger, IAutoObjectMappingProvider mapper,
            IRepository<SwapResult, Guid> repository)
        {
            _tokenOptions = tokenOptions.Value;
            _aElfClientService = aElfClientService;
            _logger = logger;
            _mapper = mapper;
            _repository = repository;
        }

        /**
         *  start.
         */
        public async Task HandleTokenInfoAndSwap()
        {
            int pageNum = 1;
            var pairsList = await QueryTokenPairsFromChain();
            var queryTokenInfo = await ConvertTokens(pageNum);
            var items = queryTokenInfo.Items;
            while (items.Count > 0)
            {
                var tmp = items.Take(_tokenOptions.BatchAmount).ToList();
                await QueryTokenAndAssembleSwapInfosAsync(pairsList, queryTokenInfo);
                tmp.RemoveRange(0, tmp.Count);
            }
        }

        /**
         * Query supports token list and forecase price，final assembly.
         * <param name="pairsList"></param>
         * <param name="queryTokenInfo"></param>
         */
        public async Task QueryTokenAndAssembleSwapInfosAsync(PairsList pairsList,
            QueryTokenInfo queryTokenInfo)
        {
            //queryTokenInfo filter
            // queryTokenInfo.Items = queryTokenInfo.Items.Where(item => item.FeeRate.Equals(_tokenOptions.FeeRate)).ToList();

            // Swap path map. token-->path
            var pathMap = new Dictionary<string, Path>();
            var tokenList = new TokenList();

            if (pairsList.Pairs.Count <= 0)
            {
                _logger.LogError("Get token pairs error,terminate！");
            }

            // Make paireList into Map
            var tokenCanSwapMap = await DisassemblePairsListIntoMap(pairsList);

            foreach (var item in queryTokenInfo.Items)
            {
                // Handle path ，expect price,slip point percentage
                await HandleSwapPathAndTokenInfoAsync(new string[]
                {
                    item.Token0.Symbol, item.Token1.Symbol
                }, tokenCanSwapMap, pathMap, tokenList);
            }

            var pathMapTmp = new MapField<string, Awaken.Contracts.SwapExchangeContract.Path>();
            foreach (var (key, value) in pathMap)
            {
                if (value.Value == null || value.ExpectPrice == null)
                {
                    continue;
                }

                pathMapTmp[key] = new Awaken.Contracts.SwapExchangeContract.Path
                {
                    Value = {value.Value},
                    ExpectPrice = value.ExpectPrice,
                    SlipPoint = value.SlipPoint
                };
            }

            var swapTokensInput = new SwapTokensInput
            {
                PathMap = {pathMapTmp},
                SwapTokenList = tokenList
            };
            await SendTranscation(swapTokensInput);
        }

        private async Task SendTranscation(SwapTokensInput swapTokensInput)
        {
            var saveList = new List<SwapResult>();
            var transcationId = await _aElfClientService.SendTranscationAsync(_tokenOptions.SwapToolContractAddress,
                _tokenOptions.OperatorPrivateKey, ContractOperateConst.SWAP_EXCHANGE_SWAP_LP_METHOD, swapTokensInput);
            await Task.Delay(4000);
            var transactionResultDto = await _aElfClientService.QueryTranscationResultByTranscationId(transcationId);
            if (transactionResultDto.Status.Equals(CommonConst.TxStatus))
            {
                var logs = transactionResultDto.Logs.Where(l => l.Name.Contains(nameof(SwapResultEvent))).ToList();
                foreach (var logEventDto in logs)
                {
                    var swapResultEvent =
                        SwapResultEvent.Parser.ParseFrom(ByteString.FromBase64(logEventDto.NonIndexed));
                    var copy = _mapper.Map<SwapResultEvent, SwapResult>(swapResultEvent);
                    saveList.Add(copy);
                }
            }
            else
            {
                foreach (var token in swapTokensInput.SwapTokenList.TokensInfo)
                {
                    saveList.Add(new SwapResult
                    {
                        Amount = token.Amount,
                        Result = false,
                        Symbol = token.TokenSymbol,
                        IsLptoken = token.TokenSymbol.StartsWith("ALP")
                    });
                }
            }

            await _repository.InsertManyAsync(saveList);
        }


        /**
         * Disassemble pair list for Preferred swap path.
         * <param name="pairsList"></param>
         */
#pragma warning disable 1998
        private async Task<Dictionary<string, List<string>>> DisassemblePairsListIntoMap(PairsList pairsList)
#pragma warning restore 1998
        {
            var map = new Dictionary<string, List<string>>();
            var pairs = pairsList.Pairs;
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
            var balance = await _aElfClientService.QueryAsync<LPBalance>(_tokenOptions.LpTokenContractAddresses,
                _tokenOptions.OperatorPrivateKey, ContractOperateConst.LP_GET_BALANCE_METHOD,
                new GetBalanceInput
                {
                    Owner = CommonHelper.CoverntString2Address(
                        await _aElfClientService.GetAddressFromPrivateKey(_tokenOptions.OperatorPrivateKey)),
                    Symbol = lpTokenSymbol
                });
            if (balance.Amount <= 0)
            {
                return;
            }
            else
            {
                // approve
                await _aElfClientService.SendTranscationAsync(_tokenOptions.LpTokenContractAddresses,
                    _tokenOptions.OperatorPrivateKey, ContractOperateConst.LP_APPROVE_METHOD, new ApproveInput
                    {
                        Amount = balance.Amount,
                        Spender = CommonHelper.CoverntString2Address(_tokenOptions.SwapToolContractAddress),
                        Symbol = lpTokenSymbol
                    });
            }

            // Get Reserves.
            var getReservesOutput = await _aElfClientService.QueryAsync<GetReservesOutput>(
                _tokenOptions.SwapContractAddress,
                _tokenOptions.OperatorPrivateKey,
                ContractOperateConst.SWAP_GET_RESERVE_METHOD,
                new GetReservesInput
                {
                    SymbolPair = {pair}
                });
            // Get lpToken totalsupply
            var getTotalSupplyOutput = await _aElfClientService.QueryAsync<GetTotalSupplyOutput>(
                _tokenOptions.SwapContractAddress,
                _tokenOptions.OperatorPrivateKey,
                ContractOperateConst.SWAP_GET_TOTAL_SUPPLY_METHOD, new StringList
                {
                    Value = {pair}
                });
            // Amount of tokens could get from remove liquit.
            var amountsExcept = await ComputeAmountFromRemoveLiquit(balance.Amount, getReservesOutput.Results.First(),
                getTotalSupplyOutput.Results.First().TotalSupply);


            for (var i = 0; i < tokens.Length; i++)
            {
                if (tokens[i].Equals(_tokenOptions.TargetToken))
                {
                    continue;
                }

                await PreferredSwapPathAsync(tokens[i], canSwapMap, pathMap);
                long amountIn = 0;
                if (getReservesOutput.Results.First().SymbolA.Equals(tokens[i]))
                {
                    amountIn = amountsExcept.First();
                }
                else
                {
                    amountIn = amountsExcept.Last();
                }

                await BudgetTokenExpectPrice(tokens[i], pathMap, amountIn);
            }

            // Add token amount 
            tokenList.TokensInfo.Add(new Token
            {
                Amount = balance.Amount,
                TokenSymbol = lpTokenSymbol
            });
        }


#pragma warning disable 1998
        private async Task<List<long>> ComputeAmountFromRemoveLiquit(long liquidityRemoveAmount,
#pragma warning restore 1998
            ReservePairResult reserves, long totalSupply)
        {
            var result = new List<long>();
            result.Add(new BigInteger(liquidityRemoveAmount.ToString())
                .Multiply(new BigInteger(reserves.ReserveA.ToString())).Divide(new BigInteger(totalSupply.ToString()))
                .LongValue);
            result.Add(new BigInteger(liquidityRemoveAmount.ToString())
                .Multiply(new BigInteger(reserves.ReserveB.ToString())).Divide(new BigInteger(totalSupply.ToString()))
                .LongValue);
            return result;
        }


        private async Task BudgetTokenExpectPrice(string token, Dictionary<string, Path> pathMap,
            long amountIn)
        {
            var path = pathMap.GetValueOrDefault(token);
            if (path != null && path.Value != null && path.Value.Count > 0)
            {
                var expect = await _aElfClientService.QueryAsync<GetAmountOutOutput>(_tokenOptions.SwapContractAddress,
                    _tokenOptions.OperatorPrivateKey, ContractOperateConst.SWAP_GET_AMOUNT_OUT_METHOD,
                    new GetAmountsOutInput
                    {
                        Path = {pathMap[token].Value},
                        AmountIn = amountIn
                    });
                var targetTokenOut = expect.amount.Last();
                var expectPrice = new BigInteger(CommonConst.ExpansionCoefficient)
                    .Multiply(BigInteger.ValueOf(targetTokenOut))
                    .Divide(BigInteger.ValueOf(amountIn));
                // todo parameter type
                pathMap[token].ExpectPrice = expectPrice.ToString();
                pathMap[token].SlipPoint = _tokenOptions.SlipPointPercent;
            }
        }


        /**
         * Preferred appropriate swap path.
         */
        public async Task<List<string>> PreferredSwapPathAsync(string tokenSymbol,
            Dictionary<string, List<string>> canSwapMap,
            Dictionary<string, Path> pathMap)
        {
            if (!tokenSymbol.IsNullOrEmpty())
            {
                var path = pathMap.GetValueOrDefault(tokenSymbol);
                if (path != null)
                {
                    if (path.Value != null && path.Value.Count > 0)
                    {
                        return path.Value;
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

                    pathMap[tokenSymbol].Value = tmp;
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
                if (canSwapToken.Equals(_tokenOptions.TargetToken))
                {
                    pathList.Add(tmp);
                }
                else
                {
                    await RecursionHandlePath(canSwapToken, canSwapMap, tmp, pathList);
                }
            }
        }

        /**
         * Query pairs list from chain.
         * <param name="pageNum"></param>
         */
        public async Task<PairsList> QueryTokenPairsFromChain()
        {
            return await _aElfClientService.QueryAsync<PairsList>(_tokenOptions.SwapContractAddress,
                _tokenOptions.OperatorPrivateKey,
                ContractOperateConst.SWAP_PAIRS_LIST_METHOD, new Empty());
        }


        private async Task<QueryTokenInfo> ConvertTokens(int pageNum)
        {
            var queryStr = await QueryTokenList(pageNum);
            return JsonConvert.DeserializeObject<QueryTokenInfo>(queryStr);
        }

        private async Task<string> QueryTokenList(int pageNum)
        {
            // Because this api not implement pages,so get out of all records.
            // var maxResultCount = _tokenOptions.BatchAmount;
            // var skipCount = (pageNum - 1) * maxResultCount;
            var maxResultCount = 999;
            var skipCount = 0;
            try
            {
                string response = null;
                string statusCode;
                do
                {
                    // response = HttpClientHelper.GetResponse(string.Format(_tokenOptions.QueryTokenUrl,maxResultCount,skipCount,_tokenOptions.FeeRate), out statusCode);
                    response = HttpClientHelper.GetResponse(
                        string.Format(_tokenOptions.QueryTokenUrl, maxResultCount, skipCount, _tokenOptions.FeeRate),
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