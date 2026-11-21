namespace LosTresMosqueteros.Integrations;

/// <summary>Resultado de una llamada al proveedor externo.</summary>
public enum ExternalApiOutcome
{
    /// <summary>El proveedor respondió con datos.</summary>
    Found = 1,

    /// <summary>El proveedor respondió, pero sin producto o sin resultados.</summary>
    NotFound = 2,

    /// <summary>El proveedor rechazó la llamada por límite de solicitudes (429).</summary>
    RateLimited = 3,

    /// <summary>Error HTTP, tiempo agotado o respuesta inválida del proveedor.</summary>
    Unavailable = 4,

    /// <summary>Error inesperado dentro de la aplicación al llamar al proveedor.</summary>
    Error = 5
}
