using System;
using System.Threading.Tasks;
using Awaken.Scripts.Dividends.Options;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using AElf.Client.Proto;

namespace Awaken.Scripts.Dividends.Services;

public interface IAElfTokenService
{
    Task<long> GetBalanceAsync(string operatorKey, Address user, string symbol);
    Task<long> GetAllowanceAsync(string operatorKey, Address owner, Address spender, string symbol);
    Task<string> ApproveTokenAsync(string operatorKey, string spender, long amount, string symbol);
}

public class AElfTokenService : IAElfTokenService, ITransientDependency
{
    private string _aelfTokenAddress;
    private readonly IAElfClientService _clientService;

    public AElfTokenService(IAElfClientService clientService, IOptionsSnapshot<DividendsScriptOptions> tokenOptions)
    {
        _clientService = clientService;
        _aelfTokenAddress = tokenOptions.Value.AElfTokenContractAddresses;
    }

    private async Task EnsureInitializeTokenAddressAsync()
    {
        if (_aelfTokenAddress.IsNullOrEmpty())
        {
            _aelfTokenAddress = await _clientService.GetAddressByNameAsync("AElf.ContractNames.Token");
        }
    }

    public async Task<long> GetBalanceAsync(string operatorKey, Address user, string symbol)
    {
        await EnsureInitializeTokenAddressAsync();
        var balanceOutput = await _clientService.QueryAsync<AElf.Client.MultiToken.GetBalanceOutput>(
            _aelfTokenAddress,
            operatorKey, ContractMethodNameConstants.GetTokenBalance,
            new AElf.Client.MultiToken.GetBalanceInput
            {
                Owner = user,
                Symbol = symbol
            });
        return balanceOutput.Balance;
    }

    public async Task<long> GetAllowanceAsync(string operatorKey, Address owner, Address spender, string symbol)
    {
        await EnsureInitializeTokenAddressAsync();
        var approveAmount = await _clientService.QueryAsync<AElf.Client.MultiToken.GetAllowanceOutput>(
            _aelfTokenAddress,
            operatorKey, ContractMethodNameConstants.GetTokenAllowance,
            new AElf.Client.MultiToken.GetAllowanceInput
            {
                Owner = owner,
                Symbol = symbol,
                Spender = spender
            });
        return approveAmount.Allowance;
    }

    public async Task<string> ApproveTokenAsync(string operatorKey, string spender, long amount, string symbol)
    {
        await EnsureInitializeTokenAddressAsync();
        var txId = await _clientService.SendTransactionAsync(
            _aelfTokenAddress,
            operatorKey, ContractMethodNameConstants.TokenApprove, new AElf.Contracts.MultiToken.ApproveInput
            {
                Symbol = symbol,
                Amount = amount,
                Spender = AElf.Types.Address.FromBase58(spender)
            });
        return txId;
    }
}