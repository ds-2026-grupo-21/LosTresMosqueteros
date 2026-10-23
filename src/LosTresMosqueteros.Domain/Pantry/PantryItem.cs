using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LosTresMosqueteros.Pantry;

public class PantryItem : AuditedAggregateRoot<Guid>
{
    public Guid OwnerId { get; private set; }
    public Guid ProductId { get; private set; }   // Id de LosTresMosqueteros.Productos.Producto
    public PantryItemStatus Status { get; private set; }
    public DateOnly? ExpirationDate { get; private set; }

    protected PantryItem()
    {
        /* Requerido por Entity Framework Core */
    }

    public PantryItem(Guid id, Guid ownerId, Guid productId, DateOnly? expirationDate = null) : base(id)
    {
        OwnerId = ownerId;
        ProductId = productId;
        Status = PantryItemStatus.Active;
        ExpirationDate = expirationDate;
    }

    public void SetExpirationDate(DateOnly? expirationDate) => ExpirationDate = expirationDate;

    public void MarkConsumed() => Status = PantryItemStatus.Consumed;
}