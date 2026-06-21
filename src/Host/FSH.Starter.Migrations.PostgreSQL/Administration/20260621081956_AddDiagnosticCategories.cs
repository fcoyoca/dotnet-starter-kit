using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddDiagnosticCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiagnosticCategories",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LegacyId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticCategories_IsActive",
                schema: "administration",
                table: "DiagnosticCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticCategories_IsDeleted",
                schema: "administration",
                table: "DiagnosticCategories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticCategories_LegacyId",
                schema: "administration",
                table: "DiagnosticCategories",
                column: "LegacyId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticCategories_Name",
                schema: "administration",
                table: "DiagnosticCategories",
                columns: new[] { "Name", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiagnosticCategories",
                schema: "administration");
        }
    }
}
