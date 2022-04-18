using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SwapExchange
{
    public class DesignTimeDbContextFactory: IDesignTimeDbContextFactory<SwapExchangeDbContext>
    {
        public SwapExchangeDbContext CreateDbContext(string[] args)
        {   
            var configuration = BuildConfiguration();
            var optionsBuilder = new DbContextOptionsBuilder<SwapExchangeDbContext>();
            optionsBuilder.UseMySql(configuration.GetConnectionString("Default"),
                MySqlServerVersion.LatestSupportedServerVersion);
            return new SwapExchangeDbContext(optionsBuilder.Options);
        }
        
        private static IConfigurationRoot BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "./"))
                .AddJsonFile("appsettings.json", optional: false);

            return builder.Build();
        }
    }
}