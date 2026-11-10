using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosTresMosqueteros.Migrations
{
    /// <inheritdoc />
    public partial class AddPantryQuantityAndUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "AppPantryItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "AppPantryItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "AppPantryItems");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "AppPantryItems");
        }
    }
}
