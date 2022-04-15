using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using Google.Protobuf;

namespace QuadraticVote.Application.Service.Extensions
{
    public static class AElfClientExtension
    {
        public static async Task<T> QueryAsync<T>(this AElfClient client, string toAddress, string methodName,
            IMessage txParameter,
            string privateKey = null, string fromAddress = null) where T : IMessage, new()
        {
            // privateKey ??= QuadraticVoteConstants.ExamplePrivateKey;
            // fromAddress ??= QuadraticVoteConstants.ExampleAddress;

            var queryTransaction =
                await client.GenerateTransactionAsync(fromAddress, toAddress, methodName,
                    txParameter);
            var txWithSignGetBalance = client.SignTransaction(privateKey, queryTransaction);
            var transactionResult = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSignGetBalance.ToByteArray().ToHex()
            });
            var dataBytes = ByteArrayHelper.HexStringToByteArray(transactionResult);
            var ret = new T();
            ret.MergeFrom(dataBytes);
            return ret;
        }
    }
}