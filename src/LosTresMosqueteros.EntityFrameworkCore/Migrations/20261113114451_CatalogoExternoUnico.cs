using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosTresMosqueteros.Migrations
{
    /// <inheritdoc />
    public partial class CatalogoExternoUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT CodigoBarras FROM AppProductos GROUP BY CodigoBarras HAVING COUNT(*) > 1)
                    THROW 51000, 'Hay codigos de barras duplicados. Revisar sus referencias antes de aplicar la migracion; no se borraron datos.', 1;
                """);
            migrationBuilder.DropIndex(
                name: "IX_AppProductos_CodigoBarras",
                table: "AppProductos");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "AppProductos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "Ingredientes",
                table: "AppProductos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Alergenos",
                table: "AppProductos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Categorias",
                table: "AppProductos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GrupoNova",
                table: "AppProductos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagenUrl",
                table: "AppProductos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Marca",
                table: "AppProductos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutriScore",
                table: "AppProductos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppProductos_CodigoBarras",
                table: "AppProductos",
                column: "CodigoBarras",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppProductos_CodigoBarras",
                table: "AppProductos");

            migrationBuilder.DropColumn(
                name: "Categorias",
                table: "AppProductos");

            migrationBuilder.DropColumn(
                name: "GrupoNova",
                table: "AppProductos");

            migrationBuilder.DropColumn(
                name: "ImagenUrl",
                table: "AppProductos");

            migrationBuilder.DropColumn(
                name: "Marca",
                table: "AppProductos");

            migrationBuilder.DropColumn(
                name: "NutriScore",
                table: "AppProductos");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "AppProductos",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Ingredientes",
                table: "AppProductos",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Alergenos",
                table: "AppProductos",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppProductos_CodigoBarras",
                table: "AppProductos",
                column: "CodigoBarras");
        }
    }
}
