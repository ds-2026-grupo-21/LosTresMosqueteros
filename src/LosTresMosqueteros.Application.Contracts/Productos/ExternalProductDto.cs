using System.Collections.Generic;
using LosTresMosqueteros.Preferences;

namespace LosTresMosqueteros.Productos;

public enum ResultadoConsultaExterna
{
    Encontrado,
    NoEncontrado,
    LimiteDeSolicitudesExcedido,
    ServicioNoDisponible
}

public class ExternalProductDto
{
    public ResultadoConsultaExterna Resultado { get; set; }
    public string? CodigoBarras { get; set; }
    public string? Nombre { get; set; }
    public string? Marca { get; set; }
    public string? ImagenUrl { get; set; }
    public string? Categorias { get; set; }
    public string? Ingredientes { get; set; }
    public List<string>? Alergenos { get; set; }
    public string? NutriScore { get; set; }
    public int? GrupoNova { get; set; }
    /// <summary>Evaluación personal del usuario actual. Se calcula al momento; no viene del proveedor.</summary>
    public FoodWarningDto? Advertencias { get; set; }
}
