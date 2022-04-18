using System;
using System.Threading.Tasks;

namespace SwapExchange.Repositories
{
    public interface IBookRepository
    {
        Task DeleteBookById(Guid id);
    }
}