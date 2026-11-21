# Métricas de integración (HU-05)

## Qué se registra

Cada llamada del servidor a Open Food Facts genera una fila en `AppExternalApiCalls` con:

| Campo | Contenido |
|---|---|
| `Operation` | `BarcodeLookup` (consulta por código) o `TextSearch` (búsqueda por texto) |
| `Outcome` | `Found`, `NotFound`, `RateLimited` (429), `Unavailable` (error HTTP, tiempo agotado o respuesta inválida) o `Error` (excepción inesperada) |
| `DurationMs` | Duración en milisegundos |
| `OccurredAt` | Momento de la llamada, en UTC |

**No se guardan** el usuario, el código o el texto consultado, tokens ni la respuesta del proveedor (regla de negocio 5).

## Cómo se registra

- `MeteredExternalProductCatalogClient` y `MeteredExternalProductSearchClient` (Application/Integrations) son decoradores de `IExternalProductCatalogClient` e `IExternalProductSearchClient`. Miden la duración, traducen el resultado del cliente y no modifican la respuesta.
- El host los registra en `LosTresMosqueterosHttpApiHostModule.ConfigureExternalCatalogClients`. Los clientes HTTP de Open Food Facts no cambiaron.
- `ExternalApiCallRecorder` guarda la fila en una **unidad de trabajo propia**: la métrica queda aunque la operación del usuario falle después, y si guardar la métrica falla, la consulta del usuario sigue y el problema queda en el log.
- Cubre consultar, buscar y guardar productos, porque los tres pasan por los mismos clientes.

## Consulta

`GET /api/app/integration-metrics?days=7` (`IntegrationMetricsAppService`), protegido con `LosTresMosqueteros.Administration.IntegrationMetrics`.

- `days` entre 1 y 90 (por defecto 7), contado hacia atrás desde ahora en UTC.
- Se agrega en la base por operación: total, con datos, sin resultados, límite de solicitudes, no disponible, errores, porcentaje de fallas, duración promedio y máxima, y última llamada. No se devuelven registros individuales.
- **Falla** = límite de solicitudes + no disponible + error. "Sin resultados" no es una falla.
- Una operación se marca como problemática con 20 % de fallas o más (`IntegrationMetricsConsts.HighFailureRatePercent`). Cualquier respuesta 429 se muestra como posible uso excesivo.
- Siempre se devuelven las dos operaciones, aunque no tengan llamadas.

## Pantalla

`/integration-metrics`, en el menú Administración. Ruta, menú y endpoint exigen el mismo permiso. Estados de carga, error con reintento, sin llamadas y datos; período de 24 horas, 7 o 30 días; una respuesta anterior no reemplaza la de un período más reciente.

## Decisiones

- Los registros no se borran automáticamente. La consulta se limita a 90 días; una limpieza periódica queda como mejora.
- La duración se mide en el servidor, alrededor de la llamada completa al cliente (incluye lectura y mapeo de la respuesta).
