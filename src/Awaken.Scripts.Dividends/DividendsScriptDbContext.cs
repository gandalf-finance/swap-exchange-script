using Awaken.Scripts.Dividends.Entities;
using Awaken.Scripts.Dividends.Helpers;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Awaken.Scripts.Dividends
{
    [ConnectionStringName("Default")]
    public class SwapExchangeDbContext : AbpDbContext<SwapExchangeDbContext>
    {
        public SwapExchangeDbContext(DbContextOptions<SwapExchangeDbContext> options) : base(options)
        {

        }

        public DbSet<SwapTransactionRecord> SwapTransactionRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            /* For background job */
            builder.ConfigureBackgroundJobs();

            builder.Entity<SwapTransactionRecord>(b =>
            {
                b.ToTable(CommonHelper.ConvertEntityNameToDb<SwapResult>());
                b.ConfigureByConvention();
            });
        }
    }
}