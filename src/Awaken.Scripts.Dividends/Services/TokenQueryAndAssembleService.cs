using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Awaken.Contracts.Swap;
using Awaken.Contracts.SwapExchangeContract;
using Awaken.Contracts.Token;
using Awaken.Scripts.Dividends.Entities;
using Awaken.Scripts.Dividends.Enum;
using Awaken.Scripts.Dividends.Extensions;
using Awaken.Scripts.Dividends.Helpers;
using Awaken.Scripts.Dividends.Options;
using Gandalf.Contracts.DividendPoolContract;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Org.BouncyCastle.Math;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Uow;
using Google.Protobuf;
using GetAllowanceInput = Awaken.Contracts.Token.GetAllowanceInput;
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
        private readonly IAutoObjectMappingProvider _mapper;
        private readonly IRepository<SwapTransactionRecord, Guid> _repository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public TokenQueryAndAssembleService(IOptionsSnapshot<DividendsScriptOptions> tokenOptions,
            IAElfClientService clientService,
            ILogger<TokenQueryAndAssembleService> logger, IAutoObjectMappingProvider mapper,
            IRepository<SwapTransactionRecord, Guid> repository, IUnitOfWorkManager unitOfWorkManager)
        {
            _dividendsScriptOptions = tokenOptions.Value;
            _clientService = clientService;
            _logger = logger;
            _mapper = mapper;
            _repository = repository;
            _unitOfWorkManager = unitOfWorkManager;
        }

        /// <summary>
        /// start
        /// </summary>
        public async Task HandleTokenInfoAndSwap()
        {
            var pairList = await QueryTokenPairsFromChain();
            if (pairList.Value.Count <= 0)
            {
                throw new Exception("Get token pairs error,terminate！");
            }

            var queryTokenInfo = await ConvertTokens();
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

            await DonateAsync(_dividendsScriptOptions.TargetToken);
            while (true)
            {
                Thread.Sleep(60000);
                if (await CheckTransactionStatusAsync() == 0)
                {
                    break;
                }
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
                    TransactionId = transactionId,
                    TransactionType = TransactionType.SwapLpTokens
                });
            }
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
                _logger.LogError($"Lp Token: {lpTokenSymbol} Balance is zero");
                return;
            }

            // approve
            var spender = _dividendsScriptOptions.SwapToolContractAddress.ToAddress();
            var approvedAmount = await _clientService.QueryAsync<Balance>(
                _dividendsScriptOptions.LpTokenContractAddresses,
                _dividendsScriptOptions.OperatorPrivateKey, ContractMethodNameConstants.GetAllowance,
                new GetAllowanceInput
                {
                    Symbol = lpTokenSymbol,
                    Owner = address,
                    Spender = spender,
                });
            var toApproveAmount = balance.Amount - approvedAmount.Amount;
            if (toApproveAmount > 0)
            {
                var txId = await _clientService.SendTransactionAsync(_dividendsScriptOptions.LpTokenContractAddresses,
                    _dividendsScriptOptions.OperatorPrivateKey, ContractMethodNameConstants.Approve, new ApproveInput
                    {
                        Amount = toApproveAmount,
                        Spender = spender,
                        Symbol = lpTokenSymbol
                    });
                await _repository.InsertAsync(new SwapTransactionRecord
                {
                    TransactionId = txId,
                    TransactionType = TransactionType.Approve
                });
            }

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
            var amountsExcept = ComputeAmountFromRemovedLiquidity(balance.Amount,
                getReservesOutput.Results.First(),
                getTotalSupplyOutput.Results.First().TotalSupply);

            foreach (var token in tokens)
            {
                if (token == _dividendsScriptOptions.TargetToken)
                {
                    continue;
                }

                await PreferredSwapPathAsync(token, canSwapMap, pathMap);
                var amountIn = getReservesOutput.Results.First().SymbolA.Equals(token)
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

        private async Task BudgetTokenExpectPrice(string token, Dictionary<string, Path> pathMap,
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
            if (tokenSymbol.IsNullOrEmpty()) return new List<string>();
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

        private async Task<QueryTokenInfo> ConvertTokens()
        {
            var queryStr = await QueryTokenList();
            return JsonConvert.DeserializeObject<QueryTokenInfo>(queryStr);
        }

        private async Task<string> QueryTokenList()
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
                        string.Format(_dividendsScriptOptions.QueryTokenUrl, maxResultCount, skipCount,
                            _dividendsScriptOptions.FeeRate),
                        out statusCode);
                } while (!statusCode.Equals("OK"));

                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return await QueryTokenList();
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

        private async Task<int> CheckTransactionStatusAsync()
        {
            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var txRecords = await _repository.GetListAsync(x => x.TransactionStatus == TransactionStatus.NotChecked);
            var toUpdateRecords = new List<SwapTransactionRecord>();
            foreach (var txRecord in txRecords)
            {
                try
                {
                    if (await HandlerSwapTransactionRecord(txRecord))
                    {
                        toUpdateRecords.Add(txRecord);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception.Message);
                    _logger.LogError(exception.StackTrace);
                }
            }

            if (toUpdateRecords.Any())
            {
                await _repository.UpdateManyAsync(toUpdateRecords);
            }

            await uow.CompleteAsync();
            return txRecords.Count;
        }

        private async Task<bool> HandlerSwapTransactionRecord(SwapTransactionRecord txRecord)
        {
            var tx = await _clientService.QueryTransactionResultByTransactionId(txRecord.TransactionId);
            if (tx.Status == DividendsScriptConstants.Mined)
            {
                txRecord.TransactionStatus = TransactionStatus.Success;
                return true;
            }

            if (tx.Status is DividendsScriptConstants.Pending or DividendsScriptConstants.PendingValidation)
            {
                return false;
            }

            _logger.LogError($"TransactionId: {tx.TransactionId} failed, message: {tx.Error}");
            txRecord.TransactionStatus = TransactionStatus.Fail;
            return true;
        }

        private async Task DonateAsync(string targetToken)
        {
            if (_dividendsScriptOptions.AElfTokenContractAddresses.IsNullOrEmpty())
            {
                _dividendsScriptOptions.AElfTokenContractAddresses = await
                    _clientService.GetAddressByNameAsync("AElf.ContractNames.Token");
            }

            var tokenContractAddress = _dividendsScriptOptions.AElfTokenContractAddresses;
            var operatorKey = _dividendsScriptOptions.OperatorPrivateKey;
            var userAddress =
                (await _clientService.GetAddressFromPrivateKey(operatorKey)).ToAddress();
            var address = GetAddress(userAddress);
            var dividendAddressBase58Str = _dividendsScriptOptions.DividendContractAddresses;
            var dividendAddress = GetAddress(dividendAddressBase58Str.ToAddress());
            var termsBlock =  _dividendsScriptOptions.TermBlocks;
            var blocksToStart = _dividendsScriptOptions.BlocksToStart;
            
            // query token balance
            var balanceOutput = await _clientService.QueryAsync<AElf.Client.MultiToken.GetBalanceOutput>(
                tokenContractAddress,
                operatorKey, ContractMethodNameConstants.GetTokenBalance,
                new AElf.Client.MultiToken.GetBalanceInput
                {
                    Owner = address,
                    Symbol = targetToken
                });
            var balance = balanceOutput.Balance;
            _logger.LogInformation($"Token: {targetToken} Balance is {balance}");
            if (balance <= 0)
            {
                return;
            }

            // approve
            var approveAmount = await _clientService.QueryAsync<AElf.Client.MultiToken.GetAllowanceOutput>(
                tokenContractAddress,
                operatorKey, ContractMethodNameConstants.GetTokenAllowance,
                new AElf.Client.MultiToken.GetAllowanceInput
                {
                    Owner = address,
                    Symbol = targetToken,
                    Spender = dividendAddress
                });
            var toApproveAmount = balance - approveAmount.Allowance;
            _logger.LogInformation($"Has approved:{approveAmount.Allowance}");
            if (toApproveAmount > 0)
            {
                _logger.LogInformation($"To approve: {toApproveAmount}");
                await _clientService.SendTransactionAsync(
                    tokenContractAddress,
                    operatorKey, ContractMethodNameConstants.TokenApprove, new AElf.Contracts.MultiToken.ApproveInput
                    {
                        Symbol = targetToken,
                        Amount = toApproveAmount,
                        Spender = dividendAddressBase58Str.ToAddress()
                    });
            }

            // new reward
            var currentHeight = await _clientService.GetCurrentHeightAsync();
            var amountPerBlock = balance / termsBlock;
            var startBlock = currentHeight + blocksToStart;
            var transactionId = await _clientService.SendTransactionAsync(
                dividendAddressBase58Str,
                operatorKey, nameof(Gandalf.Contracts.DividendPoolContract.NewReward),
                new NewRewardInput
                {
                    Tokens = { targetToken },
                    PerBlocks = { amountPerBlock },
                    Amounts = { balance },
                    StartBlock = startBlock
                });
            _logger.LogInformation(
                $"NewReward information, token: {targetToken}, start block: {startBlock}, amounts: {balance}, amount per block: {amountPerBlock}");
            if (transactionId.IsNullOrEmpty())
            {
                _logger.LogError("Failed to send newReward transaction");
                return;
            }

            await _repository.InsertAsync(new SwapTransactionRecord
            {
                TransactionId = transactionId,
                TransactionType = TransactionType.Donate
            });
        }

        private AElf.Client.Proto.Address GetAddress(AElf.Types.Address address)
        {
            var addressByteString = address.ToByteString();
            return AElf.Client.Proto.Address.Parser.ParseFrom(addressByteString);
        }
    }
}