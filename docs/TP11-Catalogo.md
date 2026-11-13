# TP11 — Parte 1: catálogo externo

Este documento describe la parte de catálogo. La integración con guardado y preferencias se detalla en [TP11-Integracion.md](TP11-Integracion.md).

Implementación de la parte 1 (RF-05 a RF-07 y RF-09), vinculada al [Issue #30](https://github.com/ds-2026-grupo-21/LosTresMosqueteros/issues/30). Agregamos búsqueda por texto y ampliamos el detalle. Verificamos la API real y el recorrido autenticado en Swagger. La revisión del equipo y el CI del PR siguen pendientes.

## Contratos y capas

Conservamos `IExternalProductCatalogClient.GetByBarcodeAsync(string)` en Application. El AppService sigue protegido con `[Authorize]` y coordina la consulta; los dos clientes HTTP permanecen en HttpApi.Host y se registran con `AddHttpClient`.

| Operación | Contrato propio | Proveedor |
| --- | --- | --- |
| `GetAsync(ConsultarProductoExternoDto)` | `ExternalProductDto` | Product Opener API v3, consulta por código |
| `SearchAsync(BuscarProductosExternosDto)` | `ExternalProductSearchResultDto` | Search-a-licious, `POST /search` |

La ruta existente de detalle es `GET /api/app/consulta-producto-externo?CodigoBarras=3017620422003`. La búsqueda utiliza `POST /api/app/consulta-producto-externo/search`. Ambas rutas quedaron confirmadas en el Swagger generado.

```json
{"texto":"nutella","pagina":1,"tamanoPagina":10}
```

Validamos en servidor un texto obligatorio de hasta 200 caracteres, página desde 1, tamaño entre 1 y 50 y `pagina × tamanoPagina <= 10000`, límite del proveedor. El texto se envía sin espacios en los extremos. Conservamos la validación previa del código de barras: texto numérico de 8 a 14 caracteres, incluidos sus ceros iniciales.

## Datos disponibles y resultados

El detalle conserva `CodigoBarras`, `Nombre`, `Marca`, `ImagenUrl` y `Resultado`, y agrega:

| Campo | Representación |
| --- | --- |
| `Categorias` | Texto informado por el proveedor; no es una lista de identificadores de taxonomía. |
| `Ingredientes` | Texto de ingredientes disponible. |
| `Alergenos` | Lista de etiquetas canónicas, por ejemplo `en:milk`. |
| `NutriScore` | Letra de `a` a `e`; desconocido o no aplicable queda `null`. |
| `GrupoNova` | Entero de 1 a 4; desconocido o fuera de ese rango queda `null`. |

Los campos omitidos o vacíos quedan `null`. Una lista de alérgenos vacía tampoco acredita ausencia de alérgenos. No inventamos un nombre ni completamos ingredientes que el proveedor no informa.

La búsqueda devuelve `Productos`, una lista de resúmenes con código, nombre, marca e imagen, y los campos `Pagina`, `TamanoPagina`, `TotalResultados`, `TotalEsExacto` y `PaginasDisponibles`. El resumen permite consultar el detalle por código. Las marcas de Search-a-licious llegan como una lista y se presentan en el campo propio `Marca` como texto separado por comas.

`TotalEsExacto = false` indica que el total informado es un límite inferior. `PaginasDisponibles` limita la navegación a las páginas accesibles dentro de los primeros 10000 resultados; con tamaño 30, como máximo 333 páginas. No se debe calcular la navegación sólo a partir de `TotalResultados`.

Conservamos los estados `Encontrado` (0), `NoEncontrado` (1), `LimiteDeSolicitudesExcedido` (2) y `ServicioNoDisponible` (3). Una búsqueda vacía válida devuelve lista vacía y total conocido; una falla devuelve metadatos de total desconocidos (`null`). Los datos parciales se reconocen por sus campos `null`, manteniendo el estado encontrado. HTTP 429 se distingue de timeout, fallas de conexión, otros errores HTTP y JSON inválido. No exponemos el cuerpo de error del proveedor.

## Configuración y registros

Los clientes utilizan las bases `https://world.openfoodfacts.org/api/v3/` y `https://search.openfoodfacts.org/`, timeout de 15 segundos y el User-Agent identificable del proyecto. Los registros propios contienen operación, duración en milisegundos y resultado; no incluyen consultas, códigos, tokens, excepciones completas ni respuestas del proveedor.

## Verificación del 10/11/2026

`dotnet test LosTresMosqueteros.slnx --configuration Release` con SDK 10.0.403: **109 pruebas correctas**, sin fallos ni omitidas. Application: 28; Domain: 5; EntityFrameworkCore: 32; HttpApi.Host: 44. La biblioteca TestBase no contiene casos ejecutables. Las pruebas automatizadas de catálogo simulan el proveedor mediante NSubstitute o HttpMessageHandler y no necesitan Internet.

Cubren resultados y fallas, páginas distintas, búsqueda vacía, totales aproximados, entradas inválidas sin consultar al proveedor, campos nuevos/ausentes, ceros iniciales y registros sin contenido sensible. Se conservan advertencias previas de compilación; no se actualizaron paquetes en esta parte.

Además probamos los clientes C# reales contra los servicios públicos, por separado de la suite:

| Entrada | Resultado observado |
| --- | --- |
| `3017620422003` | Detalle de Nutella y campos ampliados disponibles. |
| `0000000000000` | No encontrado. |
| `nutella`, páginas 1 y 2, tamaño 2 | Resultados distintos y paginación coherente. |
| `Danone`, página 1 | Resultados de la marca; imagen ausente conservada como `null` cuando corresponde. |
| `yogurts`, página 1 | Resultados y total no exacto informado por el proveedor. |
| `zzsinresultadostp112026prueba` | Búsqueda vacía con total cero. |

Estos datos externos pueden cambiar. Esta prueba verifica los adaptadores y su mapeo, pero no acredita autenticación ni el recorrido de los endpoints propios.

## API real y Swagger

El 10/11/2026 ejecutamos DbMigrator sobre una base nueva y aislada, `LosTresMosqueteros_TP11_Catalogo_Test`, en SQL Server 2019 Developer. La API de pruebas se ejecutó en `https://localhost:44314`, con esa conexión y las URLs de autoridad y Swagger configuradas únicamente para el proceso local.

Las 13 comprobaciones HTTP resultaron correctas: GET y POST anónimos devolvieron 401; con Bearer verificamos detalle encontrado y no encontrado, búsqueda por nombre (`nutella`, páginas 1 y 2), marca (`Danone`), categoría (`yogurts`) y búsqueda vacía (`zzsinresultadostp112026prueba`). Texto vacío, página cero, tamaño 51 y código no numérico devolvieron 400.

En la interfaz Swagger completamos el ingreso OAuth y ejecutamos el detalle de `3017620422003` y las dos páginas de `nutella`, tamaño 2. Las respuestas fueron 200 y las páginas devolvieron códigos diferentes. Los registros de comprobación no contienen credenciales ni tokens.

## Cobertura unitaria inicial

Agregamos `CodeCoverage.runsettings` con el recolector Microsoft Code Coverage y salida Cobertura. Incluimos Domain y Application; excluimos ensamblados de tests, contratos/DTO, infraestructura, código generado y archivos de configuración/arranque. Conservamos las reglas y casos de uso, aunque todavía no tengan pruebas unitarias.

```powershell
dotnet test test/LosTresMosqueteros.Application.Tests/LosTresMosqueteros.Application.Tests.csproj --configuration Release --settings CodeCoverage.runsettings --collect:"Code Coverage;Format=Cobertura"
dotnet test test/LosTresMosqueteros.Domain.Tests/LosTresMosqueteros.Domain.Tests.csproj --configuration Release --settings CodeCoverage.runsettings --collect:"Code Coverage;Format=Cobertura"
```

Los dos proyectos ejecutaron 28 y 5 pruebas correctas. Consolidamos las líneas por archivo y número, considerando cubierta una línea si tiene hits en cualquiera de los informes, sin duplicar archivos ni promediar porcentajes: **36 / 237 líneas = 15,19 %**. El AppService de catálogo cubre **20 / 20 líneas** en esta medición. La cifra general es una línea de base; todavía no alcanza el 70 % requerido al cierre del final.

Pendientes de M4: Marcos coordina con los autores las pruebas unitarias de PantryAppService, ProductoAppService, ExpirationWarningManager, PantryItem y ExpirationWarning, y los casos restantes de Producto. Sus pruebas de integración existentes no se cuentan como cobertura unitaria. La distribución individual de esos pendientes debe acordarse con el equipo.

[Configuración del recolector Microsoft](https://learn.microsoft.com/en-us/visualstudio/test/customizing-code-coverage-analysis).

## Pendientes para integrar

- Completar con el equipo los pendientes de cobertura unitaria señalados arriba.
- Acordar estos contratos con Jesyk y Lara antes de integrar. Jesyk podrá reutilizar la interfaz de consulta por código para guardar el detalle; el tratamiento del producto sin nombre corresponde a J1. Lara tendrá ingredientes y etiquetas de alérgenos para la comparación posterior.
- Revisar el PR hacia `main` y verificar su CI antes de integrar. La medición inicial de cobertura quedó registrada; su ampliación se coordina en el apartado común del TP11.

Las pantallas Angular de búsqueda/detalle corresponden a M2. No se modificaron el guardado, la base de datos ni las preferencias de las otras partes.

## Referencias

- [API de Search-a-licious](https://openfoodfacts.github.io/search-a-licious/users/ref-openapi/).
- [Tutorial y metadatos de paginación](https://openfoodfacts.github.io/search-a-licious/users/tutorial/).
- [API de Product Opener](https://openfoodfacts.github.io/documentation/docs/Product-Opener/api/).
- Consigna interna TP11, parte 1 y comprobaciones comunes; especificación funcional RF-05 a RF-09 y guía técnica de la cátedra.
