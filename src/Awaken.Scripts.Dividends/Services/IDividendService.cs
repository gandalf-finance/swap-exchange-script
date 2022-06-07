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
    private string _dividendContractAddress;
    private long _blocksPerTerm;
    private long _blocksToStart;
    private readonly ILogger<DividendService> _logger;


    public DividendService(IAElfClientService clientService, IOptionsSnapshot<DividendsScriptOptions> tokenOptions,
        ILogger<DividendService> logger)
    {
        _dividendContractAddress = tokenOptions.Value.DividendContractAddresses;
        _blocksPerTerm = tokenOptions.Value.BlocksPerTerm;
        _blocksToStart = tokenOptions.Value.BlocksToStart;
        _clientService = clientService;
        _logger = logger;
        CheckParameters();
    }

    private void CheckParameters()
    {
        if (_dividendContractAddress.IsNullOrEmpty())
        {
            throw new Exception("Lack of dividend contract address");
        }

        if (_blocksPerTerm <= 0)
        {
            throw new Exception($"Invalid blocksPerTerm : {_blocksPerTerm}");
        }

        if (_blocksToStart <= 0)
        {
            throw new Exception($"Invalid blocksToStart : {_blocksToStart}");
        }
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