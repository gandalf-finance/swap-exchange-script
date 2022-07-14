using System.Threading.Tasks;
using Awaken.Scripts.Dividends.Handlers.Events;
using Awaken.Scripts.Dividends.Providers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Awaken.Scripts.Dividends.Handlers;

public class ToSwapTokenEventHandler : ILocalEventHandler<ToSwapTokenEvent>,
    ITransientDependency
{
    private readonly INewRewardStateProvider _newRewardStateProvider;

    public ToSwapTokenEventHandler(INewRewardStateProvider newRewardStateProvider)
    {
        _newRewardStateProvider = newRewardStateProvider;
    }

    public Task HandleEventAsync(ToSwapTokenEvent @event)
    {
        _newRewardStateProvider.Set(@event.Id, @event.TransactionId);
        return Task.CompletedTask;
    }
}