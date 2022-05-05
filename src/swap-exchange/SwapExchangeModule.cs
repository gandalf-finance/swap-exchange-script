using System.Collections.Generic;
using System.Linq;
using AElf.Client.Service;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using SwapExchange.Jobs;
using SwapExchange.Options;
using SwapExchange.Service;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.MySQL;

namespace SwapExchange
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpBackgroundWorkersModule),
        typeof(AbpCachingStackExchangeRedisModule),
        typeof(AbpEntityFrameworkCoreMySQLModule)
    )]
    [DependsOn(typeof(AbpCachingStackExchangeRedisModule))]
    public class SwapExchangeModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            var hostEnvironment = context.Services.GetSingletonInstance<IHostEnvironment>();
            context.Services.AddHostedService<SwapExchangeHostedService>();

            // Config Mysql
            context.Services.AddAbpDbContext<SwapExchangeDbContext>(builder =>
            {
                builder.AddDefaultRepositories(true);
            });

            
            Configure<AbpDbContextOptions>(options => { options.UseMySQL(); });

            context.Services.AddSingleton<AElfClient>(new AElfClient(configuration["AElfNode:Url"]));
            
            Configure<TokenOptions>(options =>
            {
                configuration.GetSection("TokenConfig").Bind(options);
                // feeTo
                // options.OperatorPrivateKey = tokenConfig.GetSection("OperatorPrivateKey").Value;
                // options.QueryTokenUrl = tokenConfig.GetSection("QueryTokenUrl").Value;
                // options.SlipPointPercent = int.Parse(tokenConfig.GetSection("SlipPointPercent").Value);
                // options.SwapContractAddress = tokenConfig.GetSection("SwapContractAddress").Value;
                // options.SwapToolContractAddress = tokenConfig.GetSection("SwapToolContractAddress").Value;
                // options.LpTokenContractAddresses = tokenConfig.GetSection("LpTokenContractAddresses").Value;
                // options.TargetToken = tokenConfig.GetSection("TargetToken").Value;
                // options.LargeCurrencyTokens = new List<string>(tokenConfig.GetSection("LargeCurrencyTokens").Value.Split(","));
            });
        }


        public override void OnApplicationInitialization(
            ApplicationInitializationContext context)
        {
            context.AddBackgroundWorker<SwapExchangeWorker>();
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