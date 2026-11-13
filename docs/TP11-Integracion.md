# TP11 — Catálogo, guardado y preferencias

Integramos el catálogo de Marcos y el backend de preferencias de Lara, completamos su interfaz y agregamos el guardado interno. El Issue #30 conserva el checklist del grupo. La aprobación de otra integrante y el CI del PR son necesarios antes de integrar en `main`.

## Operaciones

Todas las operaciones siguientes requieren autenticación.

| Operación | Endpoint |
| --- | --- |
| Consultar catálogo por código | `GET /api/app/consulta-producto-externo?CodigoBarras=3017620422003` |
| Buscar catálogo | `POST /api/app/consulta-producto-externo/search` |
| Guardar producto externo | `POST /api/app/guardar-producto-externo/save` |
| Recuperar producto interno | `GET /api/app/producto/{id}` |
| Consultar/guardar preferencias propias | `GET /api/app/preference`, `PUT /api/app/preference` |
| Consultar/editar perfil de ABP | `GET /api/account/my-profile`, `PUT /api/account/my-profile` |

La consulta externa y la búsqueda no guardan productos. El guardado recibe `{"codigoBarras":"3017620422003"}` y devuelve `resultado` y `producto`. Si el código ya está guardado devuelve el mismo identificador, sin volver a consultar al proveedor. No crea un ítem de despensa.

`resultado` conserva los valores del catálogo: 0 encontrado, 1 no encontrado, 2 límite de solicitudes y 3 servicio no disponible. En los tres últimos casos `producto` queda `null` y no se inserta una fila.

## Persistencia y datos ausentes

El producto externo puede no tener nombre. Conservamos `Nombre = null` y ampliamos sus columnas de texto para guardar la información externa sin truncarla. La creación y edición manual mantienen sus validaciones previas de nombre obligatorio y longitudes de entrada.

Persistimos marca, imagen, categorías, ingredientes, alérgenos, Nutri-Score y NOVA cuando existen. Los alérgenos externos conservan sus etiquetas canónicas separadas por `, ` en el campo de texto existente; un valor ausente permanece `null`. Las preferencias son textos elegidos por el usuario. La comparación entre preferencias, ingredientes y taxonomías corresponde al bloque posterior.

El índice único de código protege también las altas manuales y los cambios de código. Cada escritura se ejecuta en una unidad de trabajo independiente; ante una colisión de inserciones se descarta el contexto fallido y se recupera el registro existente desde otro contexto. Una edición en conflicto devuelve un error comprensible y conserva los valores previos.

Las migraciones se aplican en orden:

1. `Added_UserPreferences`: preferencias por usuario y entradas a vigilar.
2. `CatalogoExternoUnico`: detalle externo, nombre opcional e índice único por código.
3. `FotoOpcionalPerfil`: foto opcional de cada usuario.
4. `ComparacionOrdinalPreferencias`: comparación binaria de las claves normalizadas, consistente con el código. Mantiene distintos `œuf` y `oeuf`; mayúsculas y acentos se normalizan antes de guardar.

Antes de modificar el catálogo, la migración comprueba si existen códigos duplicados. Si los encuentra, se detiene con un mensaje para revisarlos y conserva los productos y sus referencias. No elimina ni fusiona datos automáticamente.

## Perfil y preferencias

En `/preferences` se pueden agregar y quitar hasta 50 entradas de 100 caracteres, cambiar la opción de alertas y guardar. La opción comienza activada. Las entradas se recortan, se colapsan espacios y se evitan repetidos sin distinguir mayúsculas ni acentos. Una lista vacía elimina las entradas y conserva la opción elegida.

El propietario sale de `ICurrentUser`; ningún identificador enviado por el cliente selecciona otro propietario. El formulario diferencia carga, vacío, datos y error, evita envíos simultáneos y conserva los cambios si falla el guardado. Una entrada todavía sin agregar debe agregarse o borrarse antes de guardar.

La foto es opcional: PNG o JPEG de hasta 80000 bytes, estática y de hasta 16 megapíxeles. El servidor comprueba el formato y decodifica la imagen completa con SkiaSharp; rechaza datos corruptos o truncados sin modificar las preferencias. El límite de píxeles acota la memoria de decodificación. Incluimos los binarios nativos para Linux, además de Windows. La foto se guarda junto con las preferencias y se puede quitar. Nombre y correo siguen usando el perfil de ABP, accesible desde el enlace «Editar mi perfil».

Los textos propios están en español e inglés. La comparación de preferencias con productos y la aplicación de alertas quedan para el siguiente bloque.

## Comprobaciones del 13/11/2026

Usamos SQL Server 2019 Developer y bases nuevas de pruebas, sin modificar las bases habituales de desarrollo. Las credenciales y tokens no forman parte de las evidencias.

| Caso | Resultado |
| --- | --- |
| Migraciones y repetición de DbMigrator | Correctas en SQL Server. Índices únicos verificados. |
| Base anterior con dos códigos duplicados y dos referencias | Migración rechazada con error 51000; se conservaron los dos productos y las dos referencias. |
| Preferencias anónimas: GET y PUT | HTTP 401 en los endpoints reales de ABP. |
| Dos usuarios y propietario enviado por query/cuerpo | Cada sesión obtiene y cambia únicamente sus preferencias. |
| Preferencias válidas, repetidas y vacías | Guardado, normalización, recuperación y vaciado correctos. |
| Datos inválidos y foto no admitida | HTTP 400; datos previos conservados. |
| Nueva sesión | Preferencias y opción de alertas persistidas. |
| Perfil de ABP | Edición y recuperación de nombre/correo por API; guardado de nombre comprobado en Angular. |
| Guardado anónimo / código inválido | HTTP 401 / 400. |
| Cuatro guardados simultáneos de `3017620422003` | Cuatro respuestas correctas, mismo identificador y una sola fila en SQL Server. |
| Producto `0000000000000` | No encontrado y sin producto persistido. |
| Edición de código en conflicto | Respuesta controlada, sin cambios parciales. |
| Angular real | Agregar, rechazar duplicados, guardar y recuperar tras recarga; foto cargada desde la API. |
| Idioma y móvil | Español e inglés comprobados; a 375 píxeles no hubo desborde horizontal. |
| Suite integrada y build | 154 tests backend y 43 Angular correctos; build de producción Angular correcto. Se conserva la advertencia previa de tamaño del SCSS de Home. |
| Regresión de concurrencia | Dos guardados leen el código inexistente, intentan insertar con unidades independientes y recuperan una colisión real: mismo identificador y una sola fila. Prueba automatizada sin Internet. |
| Preferencias en SQL Server tras la corrección | `œuf` y `oeuf` se guardan y recuperan por HTTP; `Maní`/`MANI` se deduplican. Índice único y colación binaria verificados. |
| Fotos tras la corrección | PNG completo aceptado; PNG/JPEG truncados devuelven 400 sin cambiar los datos previos. Foto y preferencias persisten en una nueva sesión; quitar la foto conserva las entradas. Siete comprobaciones HTTP/SQL correctas. |

Las pruebas unitarias e integración automatizadas simulan el proveedor y no dependen de Internet. Las comprobaciones HTTP contra Open Food Facts se realizaron por separado; sus resultados pueden cambiar con los datos del proveedor. La prueba de cookie del proyecto HttpApi.Host.Tests utiliza rutas mínimas y no se presenta como validación de la API real.

## Cobertura y continuidad

La medición inicial con `CodeCoverage.runsettings`, anterior a las correcciones de revisión, consolidó **131 / 355 líneas = 36,90 %** con Application (35 tests) y Domain (25 tests). Se cuentan líneas de reglas y casos de uso propios sin duplicarlas entre informes. Las pruebas de integración no se incluyen en esa cifra.

Esta medición satisface la línea de base del TP11; el mínimo del 70 % corresponde al cierre del final. Marcos coordina su ampliación: completar pruebas unitarias de PantryAppService y ProductoAppService (autores Jesyk/Marcos), reglas de PantryItem y vencimientos (autores del TP08), y los casos restantes de preferencias y producto (Lara/Marcos). La asignación de cada incremento se confirma con el grupo.

Para reproducir: `dotnet test LosTresMosqueteros.slnx -c Release`; desde `angular`, `yarn test --watch=false` y `yarn build`. Para cobertura, ejecutar Application.Tests y Domain.Tests con `--settings CodeCoverage.runsettings --collect:"Code Coverage;Format=Cobertura"` y consolidar líneas por archivo y número.

Las pantallas de búsqueda/detalle, guardado y agregado a despensa, comparación de preferencias y advertencias mantienen su alcance posterior. TP11 se integra por revisión interna hacia `main`; la entrega docente final mantiene el circuito de la guía de continuidad.
