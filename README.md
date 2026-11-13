# LosTresMosqueteros

Despensa Inteligente — Trabajo Práctico Integrador de Desarrollo de Software 2026, UTN FRCU.

Aplicación con backend ABP en capas (.NET 10), SQL Server y frontend Angular 22. Cada usuario administra su despensa, su perfil y sus preferencias alimentarias.

## Integrantes

- Jesica Cassanego ([@jesyk-Daiana](https://github.com/jesyk-Daiana))
- Lara Villaverde ([@laravillaverde29](https://github.com/laravillaverde29))
- Marcos Tournoud ([@Hattingg](https://github.com/Hattingg))

## Estado de la aplicación

| Funcionalidad | Disponible |
| --- | --- |
| Ingreso y perfil | Autenticación de ABP y edición de nombre y correo en `/account/manage`. |
| Mi despensa | Listado paginado, edición de cantidad/unidad/vencimiento, consumo y eliminación con confirmación en `/pantry`. |
| Preferencias | Ingredientes o alérgenos a vigilar, opción de alertas y foto opcional en `/preferences`. |
| Catálogo externo | Consulta por código, búsqueda por texto paginada y detalle mediante la API propia. |
| Guardado de productos | Guardado por código y recuperación interna sin duplicados, separado de crear un ítem de despensa. |
| Vencimientos | Worker que registra advertencias con un umbral de tres días y fecha de referencia UTC. |

Las operaciones propias requieren autenticación. Despensa y preferencias obtienen el propietario de la sesión; un ítem ajeno responde 404. Los textos de las pantallas están disponibles en español e inglés.

Las pantallas de búsqueda/detalle y guardado del catálogo, y la comparación de preferencias con productos para mostrar advertencias, corresponden al siguiente incremento. La opción de alertas se persiste, pero todavía no controla esas advertencias.

## Requisitos

- .NET 10 SDK.
- Node.js 24.15.0 o una versión posterior de la rama 24, y Yarn 1.22.x.
- SQL Server Developer o Express en ejecución.
- ABP CLI para restaurar las bibliotecas web y generar proxies.
- Git. Visual Studio 2026 y SSMS son opcionales.

## Configuración y ejecución local

1. Configurar la misma conexión de desarrollo en `src/LosTresMosqueteros.DbMigrator/appsettings.json` y `src/LosTresMosqueteros.HttpApi.Host/appsettings.json`, o mediante la variable de entorno `ConnectionStrings__Default` en ambos procesos. No publicar credenciales ni configuraciones personales.

   Ejemplo con autenticación de Windows:

   ```json
   "ConnectionStrings": {
     "Default": "Server=localhost;Database=LosTresMosqueteros;Trusted_Connection=True;TrustServerCertificate=True"
   }
   ```

   Para una instancia Express, usar `localhost\\SQLEXPRESS` como servidor en el JSON.

2. Desde la raíz del repositorio, restaurar dependencias y preparar HTTPS local:

   ```powershell
   dotnet restore LosTresMosqueteros.slnx
   abp install-libs
   dotnet dev-certs https --trust
   ```

3. Aplicar las migraciones **desde el directorio de DbMigrator**, para que se cargue su configuración:

   ```powershell
   cd src/LosTresMosqueteros.DbMigrator
   dotnet run
   ```

   Revisar la conexión antes de ejecutarlo. Si hay códigos de barras duplicados, la migración se detiene para permitir su revisión; no elimina productos ni referencias automáticamente.

4. En otra terminal, desde la raíz, iniciar el backend:

   ```powershell
   cd src/LosTresMosqueteros.HttpApi.Host
   dotnet run
   ```

   Swagger: [https://localhost:44312/swagger](https://localhost:44312/swagger). Para ejecutar operaciones privadas, iniciar sesión mediante la autorización de Swagger o usar Bearer.

5. En otra terminal, desde la raíz, iniciar Angular:

   ```powershell
   cd angular
   yarn install --frozen-lockfile
   yarn start
   ```

   Aplicación: [http://localhost:4200](http://localhost:4200). La configuración de conexión está en `angular/src/environments/environment.ts`.

## Contratos y reglas principales

- **Despensa:** `GET/POST /api/app/pantry`, `GET/PUT/DELETE /api/app/pantry/{id}` y `POST /api/app/pantry/{id}/mark-consumed`. Cantidad, unidad y vencimiento son opcionales. La cantidad es no negativa y tiene hasta dos decimales; la interfaz limita su valor a 999.999.999.999,99 para conservar precisión. La unidad admite hasta 50 caracteres.
- **Historial:** los ítems consumidos permanecen visibles hasta su eliminación explícita. Al eliminar un ítem se eliminan sus advertencias asociadas.
- **Fechas:** el vencimiento es una fecha sin hora. El worker usa el día UTC, que puede diferir del día local cerca de medianoche.
- **Catálogo y guardado:** se conservan los datos ausentes. Guardar un código existente devuelve el mismo identificador. Consultar el catálogo no guarda productos y guardar un producto no crea un ítem de despensa.
- **Preferencias:** hasta 50 entradas de 100 caracteres, sin vacíos ni repetidos por mayúsculas o acentos. Foto opcional PNG/JPEG válida, de hasta 80000 bytes, una imagen estática y hasta 16 megapíxeles.

Los proxies están en `angular/src/app/proxy`. Si cambian los contratos, regenerarlos desde `angular` con el backend iniciado:

```powershell
abp generate-proxy -t ng --url https://localhost:44312
```

Las traducciones propias están en los JSON de `src/LosTresMosqueteros.Domain.Shared/Localization/LosTresMosqueteros`; reiniciar el backend después de modificarlas.

## Pruebas

Desde la raíz:

```powershell
dotnet test LosTresMosqueteros.slnx -c Release
```

Desde `angular`:

```powershell
yarn test --watch=false
yarn build
```

La suite utiliza proveedores externos simulados y bases aisladas. Las pruebas con SQLite no reemplazan la verificación de las migraciones en SQL Server. El guardado concurrente tiene una prueba que fuerza dos lecturas iniciales sin producto y verifica la recuperación del conflicto del índice único.

[Estado del CI](https://github.com/ds-2026-grupo-21/LosTresMosqueteros/actions). La medición unitaria se configura en `CodeCoverage.runsettings`; el mínimo del 70 % corresponde al cierre del trabajo final.

## Documentación y continuidad

- [Catálogo externo: contratos, paginación y comprobaciones](docs/TP11-Catalogo.md).
- [Integración: guardado, preferencias, migraciones y resultados](docs/TP11-Integracion.md).
- [Wiki del proyecto](https://github.com/ds-2026-grupo-21/LosTresMosqueteros/wiki).
- [Issue del incremento](https://github.com/ds-2026-grupo-21/LosTresMosqueteros/issues/30).

Integramos las ramas de funcionalidad en `main` con revisión de otro integrante y CI en verde. La guía de continuidad reserva el PR docente `main → prod` para la aplicación final completa.
