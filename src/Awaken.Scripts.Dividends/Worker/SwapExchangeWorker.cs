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
        private readonly DividendsScriptOptions _dividendsScriptOptions;

        public SwapExchangeWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
            IHandlerService service, IOptionsSnapshot<DividendsScriptOptions> tokenOptions) : base(
            timer,
            serviceScopeFactory)
        {
            Timer.Period = 1000;
            _service = service;
            _dividendsScriptOptions = tokenOptions.Value;
        }

        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            Timer.Period = 10000 * 20;
            // Timer.Period = _dividendsScriptOptions.ExecutionPeriod * 24 * 60 * 60 * 1000;
            await _service.ExecuteAsync();
        }
    }
}