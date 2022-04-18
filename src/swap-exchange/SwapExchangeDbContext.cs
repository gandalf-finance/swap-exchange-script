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
        
        public DbSet<Book> Books { get; set; }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            
            builder.Entity<Book>(b =>
            {
                b.ToTable(CommonHelper.CoverntEntityNameToDb<Book>());
                b.ConfigureByConvention();
            });
        }

       
    }
}