using System.Threading.Tasks;

namespace SwapExchange.Service
{
    public interface IQueryInfoService
    {
        Task<string> findTokenAsync();
    }
}