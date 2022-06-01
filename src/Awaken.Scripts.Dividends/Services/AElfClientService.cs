using System;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace Awaken.Scripts.Dividends.Services
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IAElfClientService))]
    public class AElfClientService : IAElfClientService, ITransientDependency
    {
        private readonly AElfClient _client;

        public AElfClientService(AElfClient client)
        {
            _client = client;
        }

        public async Task<T> QueryAsync<T>(string toAddress, string privateKey, string methodName, IMessage txParam)
            where T : IMessage, new()
        {
            var isConnectedAsync = await _client.IsConnectedAsync();
            Console.WriteLine($"aelf client status:{isConnectedAsync}");
            var address = _client.GetAddressFromPrivateKey(privateKey);
            var transaction = await _client.GenerateTransactionAsync(address, toAddress, methodName, txParam);
            var txWithSign = _client.SignTransaction(privateKey, transaction);
            var transactionResult = await _client.ExecuteTransactionAsync(new ExecuteTransactionDto()
            {
                RawTransaction = txWithSign.ToByteArray().ToHex()
            });
            var dataBytes = ByteArrayHelper.HexStringToByteArray(transactionResult);
            var ret = new T();
            ret.MergeFrom(dataBytes);
            return ret;
        }

        public async Task<string> SendTransactionAsync(string toAddress, string privateKey, string methodName,
            IMessage txParam)
        {
            var ownerAddress = _client.GetAddressFromPrivateKey(privateKey);
            var transaction = await _client.GenerateTransactionAsync(ownerAddress, toAddress, methodName, txParam);
            var signedTransaction = _client.SignTransaction(privateKey, transaction);

            var result = await _client.SendTransactionAsync(new SendTransactionInput
            {
                RawTransaction = signedTransaction.ToByteArray().ToHex()
            });
            return result.TransactionId;
        }

        public async Task<TransactionResultDto> QueryTransactionResultByTransactionId(string txId)
        {
            if (!string.IsNullOrEmpty(txId))
            {
                return await _client.GetTransactionResultAsync(txId);
            }

            return null;
        }

        public async Task<string> GetAddressFromPrivateKey(string privateKey)
        {
            return _client.GetAddressFromPrivateKey(privateKey);
        }

        public async Task<string> GetAddressByNameAsync(string name)
        {
            return (await _client.GetContractAddressByNameAsync(
                HashHelper.ComputeFrom("AElf.ContractNames.Token"))).ToBase58();
        }

        public async Task<long> GetCurrentHeightAsync()
        {
            return (await _client.GetChainStatusAsync()).BestChainHeight;
        }
    }
}