using System;
using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace Awaken.Scripts.Dividends.Providers;

public interface INewRewardStateProvider
{
    void Set(Guid id, string txId);
    bool TryToSetNewReward(string txId);
}

public class NewRewardStateProvider : INewRewardStateProvider, ISingletonDependency
{
    private Guid _currentId;
    private readonly HashSet<string> _txIds;
    private bool _isNewReward;

    public NewRewardStateProvider()
    {
        _txIds = new HashSet<string>();
    }
    
    public void Set(Guid id, string txId)
    {
        if (_currentId != id)
        {
            _txIds.Clear();
            _currentId = id;
            _isNewReward = false;
        }

        _txIds.Add(txId);
    }

    public bool TryToSetNewReward(string txId)
    {
        if (_isNewReward)
            return false;

        if (!_txIds.Contains(txId))
            return false;

        _isNewReward = true;
        return true;
    }
}