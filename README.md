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