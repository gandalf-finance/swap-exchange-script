using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Types;
using Google.Protobuf;

namespace SwapExchange.Service
{
    public interface IAElfClientService 
    {
        Task<T> QueryAsync<T>(
            string toAddress,
            string privateKey,
            string methodName,
            IMessage txParam) where T : IMessage, new();

        Task<string> SendTranscationAsync(
            string toAddress,
            string privateKey,
            string methodName,
            IMessage txParam);

        Task<TransactionResultDto> QueryTranscationResultByTranscationId(string transcationId);
        
    }
}