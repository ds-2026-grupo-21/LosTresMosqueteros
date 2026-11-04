# LosTresMosqueteros

Repositorio del Trabajo Práctico Integrador de Desarrollo de Software 2026.

## Integrantes

- Jesica Cassanego ([@jesyk-Daiana](https://github.com/jesyk-Daiana))
- Lara Villaverde ([@laravillaverde29](https://github.com/laravillaverde29))
- Marcos Tournoud ([@Hattingg](https://github.com/Hattingg))

## Requisitos

Para ejecutar el proyecto localmente se necesita:

- Visual Studio 2022 o 2026 con la carga de trabajo **Desarrollo de ASP.NET y web**.
- .NET 10 SDK.
- Node.js 24.15.0 o superior.
- Yarn 1.22.x.
- SQL Server Developer o SQL Server Express.
- SQL Server Management Studio (SSMS).
- ABP Studio.
- Git.

## Configuración local

La aplicación utiliza SQL Server mediante Entity Framework Core.

La cadena de conexión local debe configurarse en los siguientes archivos:

- `src/LosTresMosqueteros.DbMigrator/appsettings.json`
- `src/LosTresMosqueteros.HttpApi.Host/appsettings.json`

Configuración utilizada para SQL Server Express:

```json
"ConnectionStrings": {
  "Default": "Server=localhost\\SQLEXPRESS;Database=LosTresMosqueteros;Trusted_Connection=True;TrustServerCertificate=True"
}```

## Despensa: contrato del backend (TP10)

`IPantryAppService` ofrece `CreateAsync` (RF-10) y `GetListAsync`, `GetAsync`, `UpdateAsync`, `MarkConsumedAsync` y `DeleteAsync` (RF-11). El listado está paginado y ordenado por fecha de creación descendente. `PantryItemDto` incluye el identificador del ítem, el producto y su nombre, estado, cantidad, unidad y vencimiento.

Las rutas verificadas en Swagger son `GET` y `POST /api/app/pantry`, `GET`, `PUT` y `DELETE /api/app/pantry/{id}`, y `POST /api/app/pantry/{id}/mark-consumed`. El proxy generado por ABP expone estas operaciones mediante `PantryService`.

El servicio requiere autenticación; obtiene el propietario de la sesión y responde 404 tanto para ítems inexistentes como para ítems de otro usuario. Cantidad, unidad y vencimiento son opcionales. La cantidad admite valores no negativos con hasta dos decimales y 16 dígitos enteros; la unidad tiene un máximo de 50 caracteres.

Los ítems consumidos se conservan en el listado con estado `Consumed` como historial hasta que el usuario los elimine explícitamente. Al eliminar un ítem se eliminan sus advertencias asociadas. El worker utiliza la fecha UTC y un umbral de tres días.

## Mi despensa: listado Angular (TP10)

La ruta `/pantry` requiere una sesión iniciada y se accede desde el menú **Mi despensa**. El listado usa `PantryService` y `ListService` para consultar y paginar; presenta los estados de carga, error con reintento, lista vacía y resultados. Las fechas se muestran según el idioma de la aplicación. El listado muestra los valores ausentes como «No disponible» y distingue los ítems consumidos.
