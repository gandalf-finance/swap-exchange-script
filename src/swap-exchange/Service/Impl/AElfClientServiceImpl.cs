using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace SwapExchange.Service.Implemention
{   
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
            var address =  _client.GetAddressFromPrivateKey(privateKey);
            var transaction = await _client.GenerateTransactionAsync(address, toAddress, methodName, txParam);
            var txWithSign = _client.SignTransaction(privateKey, transaction);
            var transactionResult = await _client.ExecuteRawTransactionAsync(new ExecuteRawTransactionDto()
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
    }
}