using System;

namespace Awaken.Scripts.Dividends.Entities
{
    public class TokenInfo
    {
        private string Symbol { get; set; }
        private long Amount { get; set; }
        private DateTime LastDealTime { get; set; }
        private long CumulativeHandleQuantity { get; set; }
        private long CumulativeUnhandledQuantity { get; set; }
    }
}