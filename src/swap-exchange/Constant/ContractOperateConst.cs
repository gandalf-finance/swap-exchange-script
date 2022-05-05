using System;

namespace SwapExchange.Constant
{
    public class ContractOperateConst
    {   
        // Swap
        public static string SWAP_PAIRS_LIST_METHOD = "GetPairs";
        public static string SWAP_GET_AMOUNT_OUT_METHOD = "GetAmountsOut";
        public static string SWAP_GET_RESERVE_METHOD = "GetReserves";
        public static string SWAP_GET_TOTAL_SUPPLY_METHOD = "GetTotalSupply";
        
        // LP
        public static string LP_GET_BALANCE_METHOD = "GetBalance";

        public static string LP_APPROVE_METHOD = "Approve";
        // Swap Exchange
        public static string SWAP_EXCHANGE_SWAP_LP_METHOD = "SwapLpTokens";
    }
}