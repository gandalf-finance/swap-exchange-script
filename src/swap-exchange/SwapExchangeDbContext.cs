using Microsoft.EntityFrameworkCore;
using QuadraticVote.Application.Service.Extensions;
using SwapExchange.Entity;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace SwapExchange
{
    [ConnectionStringName("Default")]
    public class SwapExchangeDbContext : AbpDbContext<SwapExchangeDbContext>
    {
        public SwapExchangeDbContext(DbContextOptions<SwapExchangeDbContext> options) : base(options)
        {
            
        }
        
        // public DbSet<SwapResult> SwapResults { get; set; }
        public DbSet<SwapTranscationRecord> SwapTranscationRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // builder.Entity<SwapResult>(b =>
            // {
            //     b.ToTable(CommonHelper.CoverntEntityNameToDb<SwapResult>());
            //     b.ConfigureByConvention();
            // });

            builder.Entity<SwapTranscationRecord>(b =>
            {
                b.ToTable(CommonHelper.CoverntEntityNameToDb<SwapResult>());
                b.ConfigureByConvention();
            });
            
        }

       
    }
}