namespace LosTresMosqueteros.Productos;

public class ExternalProductSummaryDto
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public string? Marca { get; set; }
    public string? ImagenUrl { get; set; }
}
