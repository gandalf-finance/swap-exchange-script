using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuadraticVote.Application.Service.Extensions;
using SwapExchange.Entity;
using SwapExchange.Service.Implemention;

namespace SwapExchange
{
    public class Test
    {
        // static void Main(string[] args)
        // {
        //     var dir = AppDomain.CurrentDomain.BaseDirectory;
        //     var currentDirectory = System.IO.Directory.GetCurrentDirectory();
        //
        //     Console.WriteLine(currentDirectory);
        //     var workConfigPathDir = dir + "/workconfig.json";
        //
        //     var streamReader = new StreamReader(workConfigPathDir);
        //
        //     var readToEnd = streamReader.ReadToEnd();
        //
        //
        //     // var readAllText = System.IO.File.ReadAllText(workConfigPathDir);
        //     Console.WriteLine(readToEnd);
        //     var deserializeObject = JsonConvert.DeserializeObject(readToEnd);
        //     readToEnd= readToEnd + "123";
        //     TxtReadWriteHelper.Write(workConfigPathDir,readToEnd);
        // }
        // public static void Main(string[] args)
        // {
            //     // var response = HttpClientHelper.GetResponse("https://www.baidu.com/", out string statusCode);
        //     // var response = HttpClientHelper.PostResponse("https://www.baidu.com/", "null", out string statusCode);
        //     // Console.WriteLine(statusCode);
        //     // Console.WriteLine(response.ToString());
        //
        //     // var coverntEntityNameToDb = CommonHelper.CoverntEntityNameToDb<TokenHandleResult>();
        //     // Console.WriteLine(coverntEntityNameToDb);
        //     string url =
        //         "https://test.awaken.finance/api/app/trade-pairs?chainId=1ddac557-9bc6-11ec-a14b-0ee50f750b74&maxResultCount=999&skipCount=20";
        //     
        //     var response = HttpClientHelper.GetResponse(
        //         url,
        //         out string statusCode);
        //     Console.WriteLine(statusCode);
        //     var jObject = (JObject) JsonConvert.DeserializeObject(response);
        //     // Console.WriteLine(jObject["totalCount"]);
        //     // var jToken = (JArray)jObject["items"];
        //     // foreach (var token in jToken)
        //     // {
        //     //     var o = (JObject) token;
        //     //     Console.WriteLine("token0:"+o["token0"]);
        //     //     Console.WriteLine("token1:"+o["token1"]);                
        //     // }
        //     // var queryTokenInfo = HttpClientHelper.GetResponse<QueryTokenInfo>(url);
        //     // Console.WriteLine(queryTokenInfo);
        //
        //     // var tokenQueryAndAssembleServiceImpl = new TokenQueryAndAssembleServiceImpl();
        //     // var queryTokenList = tokenQueryAndAssembleServiceImpl.QueryTokenList();
        //     // Console.WriteLine(queryTokenList);
        //
        //     // string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        //     // var random = new Random();
        //     // var pairsList = new List<string>();
        //     //
        //     // for (int i = 0; i < 50; i++)
        //     // {
        //     //     var index0 = random.Next(26);
        //     //     string first = letters[index0].ToString() + index0;
        //     //     string second;
        //     //     do
        //     //     {
        //     //         var index1 = random.Next(26);
        //     //         second = letters[index1].ToString() + index1;
        //     //     } while (first.Equals(second));
        //     //
        //     //     var strs = new string[] {first, second};
        //     //     var array = strs.OrderBy(c => c).ToArray();
        //     //     string pair = array[0] + "-" + array[1];
        //     //     if (!pairsList.Contains(pair))
        //     //     {
        //     //         pairsList.Add(pair);
        //     //     }
        //     // }
        //     //
        //     // var canMap = DisassemblePairsListIntoMap(pairsList);
        //     //
        //     // PreferedSwapPathAsync("U20", canMap, new Dictionary<string, List<string>>());
        //     //
        //     // Console.WriteLine(canMap);
        //     string queryStr="";
        //     JsonConvert.DeserializeObject<QueryTokenInfo>(queryStr);
        // }


        private static Task<List<string>> PreferedSwapPathAsync(string tokenSymbol,
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
                var tmp = path.Select(s =>s ).ToList();
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
}