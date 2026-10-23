using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;

namespace LosTresMosqueteros.Pantry;

public class ExpirationWarningManager : DomainService
{
    // Se incluyen el día límite, hoy y los vencidos. La fecha recibida es UTC.
    public const int WarningThresholdDays = 3;

    private readonly IRepository<PantryItem, Guid> _items;
    private readonly IRepository<ExpirationWarning, Guid> _warnings;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ExpirationWarningManager(
        IRepository<PantryItem, Guid> items,
        IRepository<ExpirationWarning, Guid> warnings,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _items = items;
        _warnings = warnings;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public virtual async Task ProcessAsync(DateOnly todayUtc, CancellationToken cancellationToken = default)
    {
        Guid[] itemIds;
        using (var readUnitOfWork = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false))
        {
            itemIds = (await _items.GetListAsync(includeDetails: false, cancellationToken: cancellationToken))
                .Select(x => x.Id).ToArray();
            await readUnitOfWork.CompleteAsync(cancellationToken);
        }

        foreach (var itemId in itemIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await ProcessItemAsync(itemId, todayUtc, cancellationToken);
            }
            catch (Exception exception) when (exception.GetBaseException() is DbException)
            {
                // Otra ejecución puede haber insertado la misma clave única.
                // Releemos con otra unidad de trabajo; un segundo error se propaga.
                Logger.LogWarning(exception, "Reintentamos el procesamiento del ítem {PantryItemId}.", itemId);
                await ProcessItemAsync(itemId, todayUtc, cancellationToken);
            }
        }
    }

    private async Task ProcessItemAsync(Guid itemId, DateOnly todayUtc, CancellationToken cancellationToken)
    {
        using var unitOfWork = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
        var item = await _items.FindAsync(itemId, includeDetails: false, cancellationToken: cancellationToken);
        if (item is null)
        {
            await unitOfWork.CompleteAsync(cancellationToken);
            return;
        }

        var warning = await _warnings.FindAsync(
            x => x.PantryItemId == item.Id && x.Type == WarningType.Expiration,
            includeDetails: false, cancellationToken: cancellationToken);
        var daysRemaining = item.ExpirationDate?.DayNumber - todayUtc.DayNumber;
        var shouldWarn = item.Status == PantryItemStatus.Active
            && daysRemaining.HasValue && daysRemaining.Value <= WarningThresholdDays;

        if (warning is null)
        {
            if (shouldWarn)
            {
                await _warnings.InsertAsync(
                    new ExpirationWarning(GuidGenerator.Create(), item.Id,
                        item.ExpirationDate!.Value, daysRemaining!.Value),
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            if (item.ExpirationDate.HasValue)
            {
                warning.Refresh(item.ExpirationDate.Value, daysRemaining!.Value);
            }
            if (!shouldWarn)
            {
                warning.Deactivate();
            }
            await _warnings.UpdateAsync(warning, cancellationToken: cancellationToken);
        }

        await unitOfWork.CompleteAsync(cancellationToken);
    }
}
