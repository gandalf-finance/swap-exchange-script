using System;

namespace SwapExchange.Entity
{
    public class TokenInfo
    {
        private string Symbol { get; set; }
        private long Amount { get; set; }
        private DateTime LastDealTime { get; set; }
        private long CumulativeHandleQuantity { get; set; }
        private long CumulativeUnhandleQuantity { get; set; }
    }
}