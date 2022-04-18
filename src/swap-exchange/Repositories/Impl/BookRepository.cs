using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwapExchange.Entity;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace SwapExchange.Repositories.Impl
{
    public class BookRepository:EfCoreRepository<SwapExchangeDbContext,Book,Guid>,IBookRepository
    {
        public BookRepository(IDbContextProvider<SwapExchangeDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task DeleteBookById(Guid id)
        {
            var dbContext = await GetDbContextAsync();
            await dbContext.Database.ExecuteSqlRawAsync($"Delete from Books Where id ={(Guid) id}");
        }
    }
}