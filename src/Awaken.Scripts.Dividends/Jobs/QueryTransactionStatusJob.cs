using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awaken.Scripts.Dividends.Entities;
using Awaken.Scripts.Dividends.Enum;
using Awaken.Scripts.Dividends.Jobs.Descriptions;
using Awaken.Scripts.Dividends.Options;
using Awaken.Scripts.Dividends.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Awaken.Scripts.Dividends.Jobs;

public class TransactionStatusQueryJob : IAsyncBackgroundJob<TransactionStatusQueryJobDescription>, ITransientDependency
{
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IAElfClientService _clientService;
    private readonly IRepository<SwapTransactionRecord, Guid> _repository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly int _checkInternal;
    private readonly ILogger _logger;

    public TransactionStatusQueryJob(IBackgroundJobManager backgroundJobManager,
        IRepository<SwapTransactionRecord, Guid> repository, ILogger<TransactionStatusQueryJob> logger,
        IUnitOfWorkManager unitOfWorkManager, IAElfClientService clientService,
        IOptionsSnapshot<DividendsScriptOptions> options)
    {
        _backgroundJobManager = backgroundJobManager;
        _repository = repository;
        _logger = logger;
        _unitOfWorkManager = unitOfWorkManager;
        _clientService = clientService;
        _checkInternal = options.Value.TransactionCheckTerm;
    }

    public async Task ExecuteAsync(TransactionStatusQueryJobDescription args)
    {
        await CheckTransactionStatusAsync(args);
    }

    private async Task CheckTransactionStatusAsync(TransactionStatusQueryJobDescription args)
    {
        using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
        var txRecords = await _repository.GetListAsync(x =>
            x.TransactionId == args.TransactionId && x.TransactionStatus == TransactionStatus.NotChecked);
        var toUpdateRecords = new List<SwapTransactionRecord>();
        foreach (var txRecord in txRecords)
        {
            try
            {
                if (await HandlerSwapTransactionRecordAsync(txRecord))
                {
                    toUpdateRecords.Add(txRecord);
                }
                else
                {
                    await _backgroundJobManager.EnqueueAsync(args, delay: TimeSpan.FromSeconds(_checkInternal));
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.Message);
                _logger.LogError(exception.StackTrace);
            }
        }

        if (toUpdateRecords.Any())
        {
            await _repository.UpdateManyAsync(toUpdateRecords);
        }

        await uow.CompleteAsync();
    }

    private async Task<bool> HandlerSwapTransactionRecordAsync
        (SwapTransactionRecord txRecord)
    {
        var tx = await _clientService.QueryTransactionResultByTransactionId(txRecord.TransactionId);
        if (tx.Status == DividendsScriptConstants.Mined)
        {
            txRecord.TransactionStatus = TransactionStatus.Success;
            return true;
        }

        if (tx.Status is DividendsScriptConstants.Pending or DividendsScriptConstants.PendingValidation)
        {
            return false;
        }

        _logger.LogError($"TransactionId: {tx.TransactionId} failed, message: {tx.Error}");
        txRecord.TransactionStatus = TransactionStatus.Fail;
        return true;
    }
}