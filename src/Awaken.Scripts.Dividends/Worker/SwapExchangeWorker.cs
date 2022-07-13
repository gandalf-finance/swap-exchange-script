using System.Threading.Tasks;
using Awaken.Scripts.Dividends.Options;
using Awaken.Scripts.Dividends.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace Awaken.Scripts.Dividends.Worker
{
    public class SwapExchangeWorker : AsyncPeriodicBackgroundWorkerBase
    {
        private readonly IHandlerService _service;
        private readonly int _term;
        private readonly bool _isNewReward;

        public SwapExchangeWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
            IHandlerService service, IOptionsSnapshot<ScriptExecuteOptions> scriptOptions) : base(
            timer,
            serviceScopeFactory)
        {
            var scriptExecuteOptions = scriptOptions.Value;
            var firstExecute = scriptExecuteOptions.FirstExecuteSeconds - scriptExecuteOptions.ExecuteOffsetSeconds;
            Timer.Period = firstExecute * 1000;
            _service = service;
            _term = scriptExecuteOptions.FixedTermSeconds;
            _isNewReward = scriptExecuteOptions.IsNewReward;
        }

        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            Timer.Period = _term * 1000;
            await _service.ExecuteAsync(_isNewReward);
        }
    }
}