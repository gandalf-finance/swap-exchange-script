using System;
using System.Threading.Tasks;
using Awaken.Scripts.Dividends.Entities;
using Awaken.Scripts.Dividends.Handlers.Events;
using Awaken.Scripts.Dividends.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;

namespace Awaken.Scripts.Dividends.Handlers;

public class NewTransactionEventHandler : ILocalEventHandler<NewTransactionEvent>,
    ITransientDependency
{
    private readonly IRepository<SwapTransactionRecord, Guid> _repository;
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly int _checkInternal;
    private readonly ILogger _logger;

    public NewTransactionEventHandler(IRepository<SwapTransactionRecord, Guid> repository,
        ILogger<NewTransactionEventHandler> logger,
        IBackgroundJobManager backgroundJobManager, IOptionsSnapshot<DividendsScriptOptions> options)
    {
        _repository = repository;
        _logger = logger;
        _backgroundJobManager = backgroundJobManager;
        _checkInternal = options.Value.TransactionCheckTerm;
    }

    public async Task HandleEventAsync(NewTransactionEvent @event)
    {
        if (@event.TransactionId.IsNullOrEmpty())
        {
            _logger.LogError(
                $"Invalid transaction, To: {@event.ToAddress}  Method: {@event.MethodName}  Message: {@event.Message}");
            return;
        }

        _logger.LogInformation(
            $"New transaction send, TransactionId:{@event.TransactionId} To: {@event.ToAddress}  Method: {@event.MethodName}\nMessage: {@event.Message}");
        var record = new SwapTransactionRecord
        {
            TransactionId = @event.TransactionId,
            MethodName = @event.MethodName,
            ToAddress = @event.ToAddress
        };

        await _repository.InsertAsync(record, true);
        await _backgroundJobManager.EnqueueAsync(record, delay: TimeSpan.FromSeconds(_checkInternal));
    }
}