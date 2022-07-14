using System.Threading.Tasks;
using Awaken.Scripts.Dividends.Extensions;
using Awaken.Scripts.Dividends.Handlers.Events;
using Awaken.Scripts.Dividends.Options;
using Awaken.Scripts.Dividends.Providers;
using Awaken.Scripts.Dividends.Services;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Awaken.Scripts.Dividends.Handlers;

public class ToNewRewardEventHandler : ILocalEventHandler<ToNewRewardEvent>,
    ITransientDependency
{
    private readonly INewRewardStateProvider _newRewardStateProvider;
    private readonly IDividendService _dividendService;
    private readonly DividendsScriptOptions _dividendsScriptOptions;
    private readonly IAElfClientService _clientService;
    private readonly IAElfTokenService _elfTokenService;
    private readonly ILogger _logger;

    public ToNewRewardEventHandler(INewRewardStateProvider newRewardStateProvider,
        ILogger<ToNewRewardEventHandler> logger, IDividendService dividendService,
        IOptionsSnapshot<DividendsScriptOptions> dividendOptions, IAElfTokenService elfTokenService,
        IAElfClientService clientService)
    {
        _newRewardStateProvider = newRewardStateProvider;
        _logger = logger;
        _dividendService = dividendService;
        _elfTokenService = elfTokenService;
        _clientService = clientService;
        _dividendsScriptOptions = dividendOptions.Value;
    }

    public async Task HandleEventAsync(ToNewRewardEvent eventData)
    {
        if (!_newRewardStateProvider.TryToSetNewReward(eventData.TransactionId))
        {
            return;
        }

        await NewRewardAsync(_dividendsScriptOptions.TargetToken);
    }

    private async Task NewRewardAsync(string targetToken)
    {
        var operatorKey = _dividendsScriptOptions.ReceiverPrivateKey;
        var userAddress =
            (await _clientService.GetAddressFromPrivateKey(operatorKey)).ToAddress();
        var address = GetAddress(userAddress);
        var dividendAddressBase58Str = _dividendsScriptOptions.DividendContractAddresses;
        var balance = await ApproveAllTokenBalanceAsync(operatorKey, address,
            dividendAddressBase58Str, targetToken);
        balance -= balance / 5;
        if (balance <= 0)
        {
            return;
        }

        // new reward
        await _dividendService.NewRewardAsync(operatorKey, targetToken, balance);
    }

    private async Task<long> ApproveAllTokenBalanceAsync(string operatorKey, AElf.Client.Proto.Address owner,
        string spenderBase58, string targetToken)
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