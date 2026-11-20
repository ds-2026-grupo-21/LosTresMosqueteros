using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Auditing;

namespace LosTresMosqueteros.Pantry;

public class UpdatePantryItemDto : IValidatableObject
{
    private string? _privateNote;
    private bool _privateNoteSpecified;

    public decimal? Quantity { get; set; }

    [StringLength(50)]
    public string? Unit { get; set; }

    public DateOnly? ExpirationDate { get; set; }

    [DisableAuditing]
    [StringLength(PantryItemConsts.MaxPrivateNoteLength)]
    public string? PrivateNote
    {
        get => _privateNote;
        set
        {
            _privateNote = value;
            _privateNoteSpecified = true;
        }
    }

    public bool TryGetPrivateNote(out string? note)
    {
        note = _privateNote;
        return _privateNoteSpecified;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Quantity < 0 || Quantity > 9999999999999999.99m)
        {
            yield return new ValidationResult(
                "La cantidad debe estar entre 0 y 9999999999999999.99.",
                new[] { nameof(Quantity) });
        }

        if (Quantity.HasValue && decimal.Round(Quantity.Value, 2) != Quantity.Value)
        {
            yield return new ValidationResult(
                "La cantidad puede tener como máximo dos decimales.",
                new[] { nameof(Quantity) });
        }
    }
}