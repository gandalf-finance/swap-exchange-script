using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SwapExchange.Service.Implemention
{
    public class QueryInfoServiceImpl:IQueryInfoService,ITransientDependency
    {
        public Task<string> findTokenAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}