using System;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace SwapExchange.Service.Implemention
{   
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IAElfClientService))]
    public class AElfClientServiceImpl : IAElfClientService, ITransientDependency
    {
        private readonly AElfClient _client;

        public AElfClientServiceImpl(AElfClient client)
        {
            _client = client;
        }
        
        /**
         * Query 
         * @return T
         */
        public async Task<T> QueryAsync<T>(string toAddress, string privateKey, string methodName, IMessage txParam) where T : IMessage, new()
        {
            var isConnectedAsync =await _client.IsConnectedAsync();
            Console.WriteLine($"aelfClent status:{isConnectedAsync}");
            var address =  _client.GetAddressFromPrivateKey(privateKey);
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
        
        /**
         * Send transcation
         * @return TransactionId
         */
        public async Task<string> SendTranscationAsync(string toAddress, string privateKey, string methodName, IMessage txParam)
        {
            var ownerAddress = _client.GetAddressFromPrivateKey(privateKey);
            var transaction = await _client.GenerateTransactionAsync(ownerAddress, toAddress, methodName, txParam);
            var txWithSign = _client.SignTransaction(privateKey, transaction);

            var result = await _client.SendTransactionAsync(new SendTransactionInput
            {
                RawTransaction = txWithSign.ToByteArray().ToHex()
            });
            return result.TransactionId;
        }
        
        /**
         * Query transcation result by transcationId
         */
        public async Task<TransactionResultDto> QueryTranscationResultByTranscationId(string transcationId)
        {
            if (!string.IsNullOrEmpty(transcationId))
            {
                return await _client.GetTransactionResultAsync(transcationId);
            }
            return null;
        }

#pragma warning disable 1998
        public async Task<string> GetAddressFromPrivateKey(string privateKey)
#pragma warning restore 1998
        {
            return _client.GetAddressFromPrivateKey(privateKey);
        }
    }
}