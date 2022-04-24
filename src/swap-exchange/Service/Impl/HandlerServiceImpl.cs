using System;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace SwapExchange.Service.Implemention
{
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
        public async void ExecuteMainTask()
        {
           _logger.LogInformation($"Start main task time：{DateTime.UtcNow.ToUniversalTime()}");
           var queryTokenAndAssembleSwapInfosAsync = await _queryAndAssembleService.QueryTokenAndAssembleSwapInfosAsync();
           _logger.LogInformation($"End main task time:{DateTime.UtcNow.ToUniversalTime()}");
        }
    }
}