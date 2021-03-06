using AElf.Client.Service;
using Awaken.Scripts.Dividends.Jobs;
using Awaken.Scripts.Dividends.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.MySQL;

namespace Awaken.Scripts.Dividends
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpBackgroundWorkersModule),
        typeof(AbpCachingStackExchangeRedisModule),
        typeof(AbpEntityFrameworkCoreMySQLModule)
    )]
    [DependsOn(typeof(AbpCachingStackExchangeRedisModule))]
    public class DividendsScriptModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            var hostEnvironment = context.Services.GetSingletonInstance<IHostEnvironment>();
            context.Services.AddHostedService<DividendsScriptHostedService>();

            // Config Mysql
            context.Services.AddAbpDbContext<SwapExchangeDbContext>(builder =>
            {
                builder.AddDefaultRepositories(true);
            });
            
            Configure<AbpDbContextOptions>(options => { options.UseMySQL(); });

            context.Services.AddSingleton<AElfClient>(new AElfClient(configuration["AElfNode:Url"]));
            
            Configure<DividendsScriptOptions>(options =>
            {
                configuration.GetSection("TokenConfig").Bind(options);
            });
        }

        public override void OnApplicationInitialization(
            ApplicationInitializationContext context)
        {
            context.AddBackgroundWorkerAsync<SwapExchangeWorker>();
        }

        private void ConfigureRedis(ServiceConfigurationContext context, IConfiguration configuration)
        {
            var config = configuration["Redis:Configuration"];
            if (string.IsNullOrEmpty(config))
            {
                return;
            }

            var redis = ConnectionMultiplexer.Connect(config);
            context.Services
                .AddDataProtection()
                .PersistKeysToStackExchangeRedis(redis, "Swap-Exchange-Script");
        }
    }
}