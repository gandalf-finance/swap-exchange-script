namespace Awaken.Scripts.Dividends.Options
{
    public class DividendsScriptOptions
    {
        public string OperatorPrivateKey { get; set; }
        public string ReceiverPrivateKey { get; set; }

        public string QueryTokenUrl { get; set; }

        public int SlipPointPercent { get; set; }

        public string SwapContractAddress { get; set; }

        public string SwapToolContractAddress { get; set; }

        public string LpTokenContractAddresses { get; set; }
        public string AElfTokenContractAddresses { get; set; }
        public string DividendContractAddresses { get; set; }

        public string TargetToken { get; set; }
        
        public int BatchAmount { get; set; }

        public string FeeRate { get; set; }
        
        public long BlocksToStart { get; set; } = 500;
        public long BlocksPerTerm { get; set; } = 172800;
        
        public int TransactionCheckTerm { get; set; } = 60;
    }
}