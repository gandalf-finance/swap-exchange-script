using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Volo.Abp;

namespace Awaken.Scripts.Dividends
{
    public class DividendsScriptHostedService : IHostedService
    {
        private readonly IAbpApplicationWithExternalServiceProvider _application;
        private readonly IServiceProvider _serviceProvider;

        public DividendsScriptHostedService(
            IAbpApplicationWithExternalServiceProvider application,
            IServiceProvider serviceProvider)
        {
            _application = application;
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _application.InitializeAsync(_serviceProvider);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _application.ShutdownAsync();
        }
    }
}
