using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddDiagnosticCategoryCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiagnosticCategoryCodes",
                schema: "administration",
                columns: table => new
                {
                    DiagnosticCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiagnosticId = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticCategoryCodes", x => new { x.DiagnosticCategoryId, x.DiagnosticId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticCategoryCodes_DiagnosticCategoryId",
                schema: "administration",
                table: "DiagnosticCategoryCodes",
                column: "DiagnosticCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiagnosticCategoryCodes",
                schema: "administration");
        }
    }
}
