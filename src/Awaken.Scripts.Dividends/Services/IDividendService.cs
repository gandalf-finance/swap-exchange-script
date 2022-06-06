using System;
using System.Threading.Tasks;
using Awaken.Contracts.DividendPoolContract;
using Awaken.Scripts.Dividends.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Awaken.Scripts.Dividends.Services;

public interface IDividendService
{
    Task<string> NewRewardAsync(string operatorKey, string symbol, long totalAmount);
}

public class DividendService : IDividendService, ITransientDependency
{
    private readonly IAElfClientService _clientService;
    private readonly string _dividendContractAddress;
    private readonly long _blocksPerTerm;
    private readonly long _blocksToStart;
    private readonly ILogger<DividendService> _logger;


    public DividendService(IAElfClientService clientService, IOptionsSnapshot<DividendsScriptOptions> tokenOptions,
        ILogger<DividendService> logger)
    {
        _dividendContractAddress = tokenOptions.Value.DividendContractAddresses;
        if (_dividendContractAddress.IsNullOrEmpty())
        {
            throw new Exception("Lack of dividend contract address");
        }

        _blocksPerTerm = tokenOptions.Value.BlocksPerTerm;
        if (_blocksPerTerm <= 0)
        {
            throw new Exception($"Invalid blocksPerTerm : {_blocksPerTerm}");
        }

        _blocksToStart = tokenOptions.Value.BlocksToStart;
        if (_blocksToStart <= 0)
        {
            throw new Exception($"Invalid blocksToStart : {_blocksToStart}");
        }

        _clientService = clientService;
        _logger = logger;
    }

    public async Task<string> NewRewardAsync(string operatorKey, string symbol, long totalAmount)
    {
        var currentHeight = await _clientService.GetCurrentHeightAsync();
        var amountPerBlock = totalAmount / _blocksPerTerm;
        var startBlock = currentHeight + _blocksToStart;
        var transactionId = await _clientService.SendTransactionAsync(
            _dividendContractAddress,
            operatorKey, nameof(Awaken.Contracts.DividendPoolContract.NewReward),
            new NewRewardInput
            {
                Tokens = { symbol },
                PerBlocks = { amountPerBlock },
                Amounts = { totalAmount },
                StartBlock = startBlock
            });
        _logger.LogInformation(
            $"NewReward information, token: {symbol}, start block: {startBlock}, amounts: {totalAmount}, amount per block: {amountPerBlock}");
        return transactionId;
    }
}