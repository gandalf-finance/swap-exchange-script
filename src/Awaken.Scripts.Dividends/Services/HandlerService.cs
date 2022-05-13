using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Awaken.Scripts.Dividends.Services
{
    public class HandlerService : IHandlerService, ITransientDependency
    {
        private readonly ILogger<HandlerService> _logger;
        private readonly ITokenQueryAndAssembleService _queryAndAssembleService;

        public HandlerService(ILogger<HandlerService> logger,
            ITokenQueryAndAssembleService queryAndAssembleService)
        {
            _logger = logger;
            _queryAndAssembleService = queryAndAssembleService;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation($"Start main task time：{DateTime.UtcNow.ToUniversalTime()}");
            await _queryAndAssembleService.HandleTokenInfoAndSwap();
            _logger.LogInformation($"End main task time:{DateTime.UtcNow.ToUniversalTime()}");
        }
    }
}