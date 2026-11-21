using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosTresMosqueteros.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalApiCalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppExternalApiCalls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<int>(type: "int", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppExternalApiCalls", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppExternalApiCalls_OccurredAt_Operation",
                table: "AppExternalApiCalls",
                columns: new[] { "OccurredAt", "Operation" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppExternalApiCalls");
        }
    }
}
