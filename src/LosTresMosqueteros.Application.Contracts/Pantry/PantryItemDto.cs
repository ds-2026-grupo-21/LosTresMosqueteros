using System;
using LosTresMosqueteros.Preferences;
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
    public string? PrivateNote { get; set; }

    /// <summary>
    /// Estado de vencimiento calculado por el servidor con el día UTC y la regla común
    /// (vencido, vence hoy, próximo entre 1 y 3 días inclusive, o sin aviso).
    /// </summary>
    public ExpirationState ExpirationState { get; set; }

    /// <summary>Días hasta el vencimiento; negativo si ya venció; null si no hay fecha.</summary>
    public int? DaysRemaining { get; set; }

    /// <summary>
    /// Evaluación personal al agregar el ítem (se calcula al momento, no se guarda).
    /// Solo se completa al crear; en listados y consultas queda en null.
    /// </summary>
    public FoodWarningDto? Advertencias { get; set; }
}