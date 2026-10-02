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
}