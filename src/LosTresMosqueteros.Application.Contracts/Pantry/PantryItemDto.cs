using System;
using Volo.Abp.Application.Dtos;

namespace LosTresMosqueteros.Pantry;

public class PantryItemDto : EntityDto<Guid>
{
    public Guid ProductId { get; set; }

    public string? ProductName { get; set; }

    public PantryItemStatus Status { get; set; }

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }

    public DateOnly? ExpirationDate { get; set; }
}