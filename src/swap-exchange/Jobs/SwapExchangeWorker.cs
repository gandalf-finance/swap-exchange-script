using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuadraticVote.Application.Service.Extensions;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.Threading;

namespace SwapExchange.Jobs
{
    public class SwapExchangeWorker : AsyncPeriodicBackgroundWorkerBase
    {
        private IDistributedCache<string> _stringCache;
        
        public SwapExchangeWorker(AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory,
            IDistributedCache<string> StringCache) : base(timer,
            serviceScopeFactory)
        {
            Timer.Period = 1000;
            _stringCache = StringCache;
        }

        protected async override Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            Logger.LogInformation("开始执行："+DateTime.Now.ToString());
            string key = "Test";
            await _stringCache.SetAsync(key, DateTime.Now.ToString(),new DistributedCacheEntryOptions
            {   
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(12),
            });
            var Dir = AppDomain.CurrentDomain.BaseDirectory;
            Console.WriteLine(Dir);
        }
    }
}