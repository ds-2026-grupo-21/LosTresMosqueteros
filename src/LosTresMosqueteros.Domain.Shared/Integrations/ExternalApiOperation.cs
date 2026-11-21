namespace LosTresMosqueteros.Integrations;

/// <summary>Operación del proveedor externo que se registra en las métricas.</summary>
public enum ExternalApiOperation
{
    /// <summary>Consulta de un producto por código de barras (Open Food Facts).</summary>
    BarcodeLookup = 1,

    /// <summary>Búsqueda por nombre, marca o categoría (Search-a-licious).</summary>
    TextSearch = 2
}
