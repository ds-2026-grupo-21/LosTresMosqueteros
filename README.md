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
}
```

## Despensa: contrato del backend (TP10)

`IPantryAppService` ofrece `CreateAsync` (RF-10) y `GetListAsync`, `GetAsync`, `UpdateAsync`, `MarkConsumedAsync` y `DeleteAsync` (RF-11). El listado está paginado y ordenado por fecha de creación descendente. `PantryItemDto` incluye el identificador del ítem, el producto y su nombre, estado, cantidad, unidad y vencimiento.

Las rutas verificadas en Swagger son `GET` y `POST /api/app/pantry`, `GET`, `PUT` y `DELETE /api/app/pantry/{id}`, y `POST /api/app/pantry/{id}/mark-consumed`. El proxy generado por ABP expone estas operaciones mediante `PantryService`.

El servicio requiere autenticación; obtiene el propietario de la sesión y responde 404 tanto para ítems inexistentes como para ítems de otro usuario. Cantidad, unidad y vencimiento son opcionales. La cantidad admite valores no negativos con hasta dos decimales y 16 dígitos enteros; la unidad tiene un máximo de 50 caracteres.

Los ítems consumidos se conservan en el listado con estado `Consumed` como historial hasta que el usuario los elimine explícitamente. Al eliminar un ítem se eliminan sus advertencias asociadas. El worker utiliza la fecha UTC y un umbral de tres días.

La relación entre advertencias e ítems impide guardar una advertencia para un ítem inexistente y elimina las advertencias asociadas al eliminarlo. Después de actualizar la rama se debe ejecutar DbMigrator. Si una base ya contiene advertencias sin ítem, es necesario revisarlas antes de aplicar esta migración.

## Mi despensa: listado Angular (TP10)

La ruta `/pantry` requiere una sesión iniciada y se accede desde el menú **Mi despensa**. El listado usa `PantryService` y `ListService` para consultar y paginar; presenta los estados de carga, error con reintento, lista vacía y resultados. Las fechas se muestran según el idioma de la aplicación. El listado muestra los valores ausentes como «No disponible» y distingue los ítems consumidos.

La fecha de vencimiento es una fecha sin hora (`DateOnly`); la pantalla muestra esa misma fecha con el formato regional y no calcula los días restantes. El worker compara la fecha de vencimiento con el día actual en UTC, por lo que el día de referencia puede diferir del día local cerca de la medianoche.

El listado usa `skipHandleError` porque muestra su propio mensaje de error localizado y ofrece reintentar; así se evita duplicar el aviso predeterminado de ABP.

Si no hay conexión durante el inicio de la aplicación, se muestra un mensaje localizado con la opción de reintentar. Una vez iniciada, se conserva el manejo habitual de errores de ABP.

## Mi despensa: edición, consumo y eliminación (TP10)

### Correspondencia con el BackEnd y el proxy

| Operación de la pantalla | Método del proxy (`PantryService`) | Endpoint |
|---|---|---|
| Listar ítems propios (paginado) | `getList` | `GET /api/app/pantry` |
| Editar cantidad, unidad y vencimiento | `update` | `PUT /api/app/pantry/{id}` |
| Marcar como consumido | `markConsumed` | `POST /api/app/pantry/{id}/mark-consumed` |
| Eliminar | `delete` | `DELETE /api/app/pantry/{id}` |

La interfaz no reemplaza la autorización del servidor: ocultar o deshabilitar botones es solo usabilidad.

Componentes: `PantryComponent` (lista, consumo y eliminación) y `PantryEditModalComponent` (formulario reactivo de edición). Los proxies están en `angular/src/app/proxy` y se generan con `abp generate-proxy -t ng`; no se editan a mano.

### Ítems consumidos

Se conservan como historial, visibles con la etiqueta "Consumido". El botón "Marcar como consumido" se oculta en ellos. Para quitarlos, el usuario los elimina de forma explícita, con confirmación previa. Después de editar, consumir o eliminar se recarga la página actual con `getWithoutPageReset`.

### Manejo de errores

- **Edición:** `skipHandleError: true`. El error se muestra dentro del modal, sin cerrarlo y sin perder lo ingresado. Un 400 muestra "el servidor rechazó algunos valores"; otro error muestra un mensaje genérico de guardado.
- **Consumo y eliminación:** `skipHandleError: true`. Se muestra un aviso localizado sobre la lista.
- No se muestran códigos de estado ni detalles técnicos al usuario.

### Validaciones

El formulario valida cantidad no negativa con hasta dos decimales y unidad de hasta 50 caracteres. Para evitar pérdida de precisión en JavaScript, la interfaz limita la cantidad a 999.999.999.999,99; el servidor conserva su propio límite y sus validaciones.

### Acceso a ítems ajenos

Un ítem de otro usuario responde 404 en todas las operaciones, igual que uno inexistente.

### Localización

Los textos usan claves con prefijo `Pantry:` en `en.json` y `es.json` (proyecto `Domain.Shared`). Después de modificarlas hay que reiniciar el host, porque Angular toma las traducciones del servidor.

### Pruebas

- Componente (Vitest): `cd angular && yarn test`. Las pruebas cubren los estados de carga, error, vacío y resultados, la edición con datos inválidos, la confirmación previa de eliminación y la cancelación sin llamada al servicio.
- BackEnd: Test Explorer en Visual Studio. La siguiente captura corresponde a una ejecución anterior de 58 pruebas. <img width="827" height="727" alt="Test Explorer: 58 pruebas aprobadas" src="https://github.com/user-attachments/assets/ef4f15c3-1486-4e06-8570-f51f246e0ddc" />

