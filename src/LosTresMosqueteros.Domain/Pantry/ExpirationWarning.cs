using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LosTresMosqueteros.Pantry;

public class ExpirationWarning : AuditedEntity<Guid>
{
    public Guid PantryItemId { get; private set; }
    public WarningType Type { get; private set; }
    public DateOnly ExpirationDate { get; private set; }  // fecha que originó el aviso
    public int DaysRemaining { get; private set; }        // negativo = vencido
    public bool IsActive { get; private set; }

    protected ExpirationWarning()
    {
        /* Requerido por Entity Framework Core */
    }

    public ExpirationWarning(Guid id, Guid pantryItemId, DateOnly expirationDate, int daysRemaining) : base(id)
    {
        PantryItemId = pantryItemId;
        Type = WarningType.Expiration;
        ExpirationDate = expirationDate;
        DaysRemaining = daysRemaining;
        IsActive = true;
    }

    public void Refresh(DateOnly expirationDate, int daysRemaining)
    {
        ExpirationDate = expirationDate;
        DaysRemaining = daysRemaining;
        IsActive = true;
    }

    public void Deactivate() => IsActive = false;
}