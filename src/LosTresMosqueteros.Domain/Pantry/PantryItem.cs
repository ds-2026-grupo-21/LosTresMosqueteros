using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LosTresMosqueteros.Pantry;

public class PantryItem : AuditedAggregateRoot<Guid>
{
    public Guid OwnerId { get; private set; }
    public Guid ProductId { get; private set; }   // Id de LosTresMosqueteros.Productos.Producto
    public PantryItemStatus Status { get; private set; }
    public DateOnly? ExpirationDate { get; private set; }
    public decimal? Quantity { get; private set; }
    public string? Unit { get; private set; }
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
    public void UpdateDetails(decimal? quantity, string? unit, DateOnly? expirationDate)
    {
        if (quantity < 0 || quantity > 9999999999999999.99m ||
            (quantity.HasValue && decimal.Round(quantity.Value, 2) != quantity.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "La cantidad debe estar entre 0 y 9999999999999999.99 y tener como máximo dos decimales.");
        }

        var normalizedUnit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();

        if (normalizedUnit?.Length > 50)
        {
            throw new ArgumentException(
                "La unidad no puede superar los 50 caracteres.",
                nameof(unit));
        }

        Quantity = quantity;
        Unit = normalizedUnit;
        ExpirationDate = expirationDate;
    }
    public void MarkConsumed() => Status = PantryItemStatus.Consumed;
}
