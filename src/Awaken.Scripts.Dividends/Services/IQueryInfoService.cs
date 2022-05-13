using System.Threading.Tasks;

namespace Awaken.Scripts.Dividends.Services
{
    public interface IQueryInfoService
    {
        Task<string> FindTokenAsync();
    }
}