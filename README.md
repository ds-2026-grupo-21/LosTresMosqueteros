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
| Mi despensa | Listado paginado, notas privadas por ítem, edición de cantidad/unidad/vencimiento, consumo y eliminación con confirmación en `/pantry`. |
| Preferencias | Ingredientes o alérgenos a vigilar, opción de alertas y foto opcional en `/preferences`. |
| Catálogo externo | Consulta por código, búsqueda por texto paginada y detalle en `/catalog`, mediante la API propia. |
| Guardado de productos | Guardado por código y agregado a la despensa desde el detalle, sin duplicar el producto interno. |
| Advertencias alimentarias | Comparación informativa con las preferencias del usuario al consultar, guardar o agregar un producto. |
| Vencimientos | Estado y aviso en Mi despensa. El worker registra los avisos con un umbral de tres días inclusive y fecha de referencia UTC. |

Las operaciones propias requieren autenticación. Despensa y preferencias obtienen el propietario de la sesión; un ítem ajeno responde 404. Los textos de las pantallas están disponibles en español e inglés.

En el detalle se informa la fuente externa y que la evaluación no constituye consejo médico. La opción de alertas controla la presentación personalizada; la ausencia de información se informa por separado.

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
- **Nota privada:** cada ítem admite una nota opcional de hasta 500 caracteres (unidades UTF-16), con saltos de línea. En `PUT /api/app/pantry/{id}`, omitir `privateNote` conserva la nota. Enviar `null`, una cadena vacía o solo espacios la elimina. El límite se valida también en el formulario de edición.
- **Historial:** los ítems consumidos permanecen visibles hasta su eliminación explícita. Al eliminar un ítem se eliminan sus advertencias asociadas.
- **Vencimientos.** La fecha se guarda sin hora. La API y el worker utilizan el día UTC y un umbral de tres días inclusive. «Mostrar alertas» controla el aviso y conserva la fecha, el estado y la nota.
- **Catálogo y guardado:** se conservan los datos ausentes. Guardar un código existente devuelve el mismo identificador. Consultar el catálogo no guarda productos y guardar un producto no crea un ítem de despensa.
- **Preferencias:** hasta 50 entradas de 100 caracteres, sin vacíos ni repetidos por mayúsculas o acentos. Foto opcional PNG/JPEG válida, de hasta 80000 bytes, una imagen estática y hasta 16 megapíxeles.
- **Advertencias:** se calculan al consultar, guardar y agregar, con las preferencias de la sesión. Se comparan palabras completas sin distinguir mayúsculas ni acentos; no se traducen ni se infieren equivalencias. Si faltan datos o no se pueden interpretar, se informa que no son verificables. El detalle del proveedor y el producto guardado pueden tener datos diferentes; sus advertencias se muestran por separado.

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

### Verificación del TP13

Pasaron 230 pruebas de .NET y 78 de Angular. Con DbMigrator y SQL Server conservamos un ítem previo con nota nula. Con dos cuentas verificamos por HTTP que cada una accede a sus propias notas y avisos. Probamos ambos en Angular y comprobamos que el worker actualiza los avisos sin duplicarlos.

La cobertura unitaria de Domain y Application pasó de 196/448 líneas (43,75 %) a 281/591 (47,55 %). La tabla de comprobaciones y el alcance de la medición están en la [Wiki del TP13](https://github.com/ds-2026-grupo-21/LosTresMosqueteros/wiki/TP13-Notas-privadas-y-avisos-de-vencimiento.#comprobaciones-del-tp13).

## Documentación y continuidad

- [Catálogo externo: contratos, paginación y comprobaciones](docs/TP11-Catalogo.md).
- [Integración: guardado, preferencias, migraciones y resultados](docs/TP11-Integracion.md).
- [Catálogo y advertencias alimentarias](docs/Catalogo-y-advertencias.md).
- [Wiki del proyecto](https://github.com/ds-2026-grupo-21/LosTresMosqueteros/wiki).
- [Notas privadas y avisos de vencimiento (issue #34)](https://github.com/ds-2026-grupo-21/LosTresMosqueteros/issues/34).

Integramos las ramas de funcionalidad en `main` con revisión de otro integrante y CI en verde. La guía de continuidad reserva el PR docente `main → prod` para la aplicación final completa.
