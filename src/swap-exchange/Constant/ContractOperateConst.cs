using System;

namespace SwapExchange.Constant
{
    public class ContractOperateConst
    {   
        // Swap
        public static readonly string SwapPairsListMethod = "GetPairs";
        public static readonly string SwapGetAmountOutMethod = "GetAmountsOut";
        public static readonly string SwapGetReserveMethod = "GetReserves";
        public static readonly string SwapGetTotalSupplyMethod = "GetTotalSupply";
        
        // LP
        public static readonly string LpGetBalanceMethod = "GetBalance";

        public static readonly string LpApproveMethod = "Approve";
        // Swap Exchange
        public static readonly string SwapExchangeSwapLpMethod = "SwapLpTokens";
    }
}