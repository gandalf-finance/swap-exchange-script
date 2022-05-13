using System.Threading.Tasks;
using AElf.Client.Dto;
using Google.Protobuf;

namespace Awaken.Scripts.Dividends.Services
{
    public interface IAElfClientService 
    {
        Task<T> QueryAsync<T>(
            string toAddress,
            string privateKey,
            string methodName,
            IMessage txParam) where T : IMessage, new();

        Task<string> SendTransactionAsync(
            string toAddress,
            string privateKey,
            string methodName,
            IMessage txParam);

        Task<TransactionResultDto> QueryTransactionResultByTransactionId(string txId);

        Task<string> GetAddressFromPrivateKey(string privateKey);
    }
}