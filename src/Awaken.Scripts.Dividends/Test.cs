using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using Awaken.Contracts.SwapExchangeContract;
using Awaken.Scripts.Dividends.Helpers;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace Awaken.Scripts.Dividends;

public class Test
{
    public static void Main(string[] args)
    {
        var client = new AElfClient("http://18.163.40.175:8000");
        var isConnectedAsync = AsyncHelper.RunSync(() => client.IsConnectedAsync());
        Console.WriteLine(isConnectedAsync);
        var privateKey = "4dbb19a4199b5183c048727ecfa01e6c6906e3fea6ffe8f17bfc079923d22b54";
        var ownerAddress = client.GetAddressFromPrivateKey(privateKey);
        var swapToolContractAddress = "UYdd84gLMsVdHrgkr3ogqe1ukhKwen8oj32Ks4J1dg6KH9PYC";
        var map = new Dictionary<string, Path>
        {
            ["ELF"] = new Path
            {
                Value = { "ELF", "USDTE" },
                ExpectPrice = "9600187043178",
                SlipPoint = 5
            }
        };
        var tokenList = new TokenList();
        tokenList.TokensInfo.Add(new Token
        {
            Amount = 5590140,
            TokenSymbol = "ALP ELF-USDTE"
        });

        var swapTokensInput = new SwapTokensInput
        {
            PathMap = { map },
            SwapTokenList = tokenList
        };
        var transaction = AsyncHelper.RunSync(() =>
            client.GenerateTransactionAsync(ownerAddress, swapToolContractAddress, "SwapLpTokens", swapTokensInput));
        var signedTransaction = client.SignTransaction(privateKey, transaction);
        var sendTransactionOutput = AsyncHelper.RunSync(() => client.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = signedTransaction.ToByteArray().ToHex()
        }));

        var txId = sendTransactionOutput.TransactionId;
        Console.WriteLine(txId);
        var transactionResultDto = AsyncHelper.RunSync(() => client.GetTransactionResultAsync(txId));
        Console.WriteLine(transactionResultDto);
    }

    private static Task<List<string>> PreferredSwapPathAsync(string tokenSymbol,
        Dictionary<string, List<string>> canSwapMap,
        Dictionary<string, List<string>> pathMap)
    {
        var path = pathMap.GetValueOrDefault(tokenSymbol) ?? new List<string>();
        var list = new List<List<string>>();
        RecursionHandlePath(tokenSymbol, canSwapMap, null, list);
        Console.WriteLine(list.ToString());
        return null;
    }

    private static Dictionary<string, List<string>> DisassemblePairsListIntoMap(List<string> pairs)
    {
        var map = new Dictionary<string, List<string>>();
        foreach (var pair in pairs)
        {
            var tokens = LpTokenHelper.ExtractTokensFromTokenPair(pair);
            var token0 = tokens[0];
            var token1 = tokens[1];
            var token0List = map.GetValueOrDefault(token0) ?? new List<string>();
            if (!token0List.Contains(token1))
            {
                token0List.Add(token1);
                map[token0] = token0List;
            }

            var token1List = map.GetValueOrDefault(token1) ?? new List<string>();
            if (!token1List.Contains(token0))
            {
                token1List.Add(token0);
                map[token1] = token1List;
            }
        }

        return map;
    }

    private static void RecursionHandlePath(string token, Dictionary<string, List<string>> canSwapMap,
        List<string> path, List<List<string>> list)
    {
        string targetToken = "A0";
        var canSwapTokens = canSwapMap[token];
        foreach (var canSwapToken in canSwapTokens)
        {
            if (path == null)
            {
                path = new List<string>();
            }

            if (path.Count == 0)
            {
                path.AddFirst(token);
            }

            if (path.Contains(canSwapToken))
            {
                continue;
            }

            // deep copy
            var tmp = path.Select(s => s).ToList();
            tmp.Add(canSwapToken);
            if (canSwapToken.Equals(targetToken))
            {
                list.Add(tmp);
            }
            else
            {
                RecursionHandlePath(canSwapToken, canSwapMap, tmp, list);
            }
        }
    }
}