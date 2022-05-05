using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace SwapExchange.Service.Implemention
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IHandlerService))]
    public class HandlerServiceImpl:IHandlerService,ITransientDependency
    {
        private readonly ILogger<HandlerServiceImpl> _logger;
        private readonly ITokenQueryAndAssembleService _queryAndAssembleService;
        
        public HandlerServiceImpl(ILogger<HandlerServiceImpl> logger, ITokenQueryAndAssembleService queryAndAssembleService)
        {
            _logger = logger;
            _queryAndAssembleService = queryAndAssembleService;
        }

        /**
         * Main task
         */
        public async Task ExecuteMainTask()
        {
           _logger.LogInformation($"Start main task time：{DateTime.UtcNow.ToUniversalTime()}");
           await _queryAndAssembleService.HandleTokenInfoAndSwap();
           _logger.LogInformation($"End main task time:{DateTime.UtcNow.ToUniversalTime()}");
        }
    }
}