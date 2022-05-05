using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuadraticVote.Application.Service.Extensions;
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
        public SwapExchangeWorker(AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory,
            IDistributedCache<string> stringCache,
            IBookService bookService, IHandlerService service) : base(timer,
            serviceScopeFactory)
        {
            Timer.Period = 1000;
            // _stringCache = stringCache;
            // _bookService = bookService;
            _service = service;
        }

        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {   
            Timer.Period = 10000 * 20;
            // string key = "Test";
            // await _stringCache.SetAsync(key, DateTime.Now.ToString(),new DistributedCacheEntryOptions
            // {   
            //     AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(12),
            // });
            // var Dir = AppDomain.CurrentDomain.BaseDirectory;
            // Console.WriteLine(Dir);
            // var id = _bookService.Save();
            // var book = _bookService.GetById(id);
            // book.Name = "Update:"+DateTime.Now.Date.ToString();
            // _bookService.Update(book);
            await _service.ExecuteMainTask();
        }
    }
}