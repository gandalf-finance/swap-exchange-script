using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuadraticVote.Application.Service.Extensions;
using SwapExchange.Options;
using SwapExchange.Service;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.Threading;

namespace SwapExchange.Jobs
{
    public class SwapExchangeWorker : AsyncPeriodicBackgroundWorkerBase
    {
        // private  IDistributedCache<string> _stringCache;
        // private  IBookService _bookService;
        private readonly IHandlerService _service;
        private readonly TokenOptions _tokenOptions;

        public SwapExchangeWorker(AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory,
            IDistributedCache<string> stringCache,
            IBookService bookService, IHandlerService service, IOptionsSnapshot<TokenOptions> tokenOptions) : base(
            timer,
            serviceScopeFactory)
        {
            Timer.Period = 1000;
            // _stringCache = stringCache;
            // _bookService = bookService;
            _service = service;
            _tokenOptions = tokenOptions.Value;
        }

        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            Timer.Period = 10000 * 20;
            // Timer.Period = _tokenOptions.ExecutionPeriod * 24 * 60 * 60 * 1000;
            await _service.ExecuteMainTask();
        }
    }
}