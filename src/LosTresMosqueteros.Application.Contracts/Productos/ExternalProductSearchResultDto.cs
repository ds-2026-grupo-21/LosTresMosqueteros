using System.Collections.Generic;

namespace LosTresMosqueteros.Productos;

public class ExternalProductSearchResultDto
{
    public ResultadoConsultaExterna Resultado { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public long? TotalResultados { get; set; }
    public bool? TotalEsExacto { get; set; }
    public int? PaginasDisponibles { get; set; }
    public List<ExternalProductSummaryDto> Productos { get; set; } = new();
}
