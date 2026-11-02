using System;

namespace LosTresMosqueteros.Pantry;

public class CreatePantryItemDto : UpdatePantryItemDto
{
    public Guid ProductId { get; set; }
}