using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Awaken.Scripts.Dividends.Services
{
    public class QueryInfoServiceImpl:IQueryInfoService,ITransientDependency
    {
        public Task<string> FindTokenAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}