using System.Threading.Tasks;

namespace Awaken.Scripts.Dividends.Services
{
    public interface ITokenQueryAndAssembleService
    {
        Task HandleTokenInfoAndSwap(bool isNewReward);
    }
}