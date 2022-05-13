using System;

namespace Awaken.Scripts.Dividends.Entities
{
    public class TokenHandleResult
    {
        private string Symbol { get; set; }
        private long SwapInAmount { get; set; }
        private long SwapOutAmount { get; set; }
        private PathInfo PathInfo { get; set; }
        private bool IsLpToken { get; set; }
        private bool HandleResult { get; set; }
        private DateTime HandleTime { get; set; }
    }
}