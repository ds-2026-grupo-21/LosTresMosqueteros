using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosTresMosqueteros.Migrations
{
    /// <inheritdoc />
    public partial class AddPantryWarningForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_AppExpirationWarnings_AppPantryItems_PantryItemId",
                table: "AppExpirationWarnings",
                column: "PantryItemId",
                principalTable: "AppPantryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppExpirationWarnings_AppPantryItems_PantryItemId",
                table: "AppExpirationWarnings");
        }
    }
}
