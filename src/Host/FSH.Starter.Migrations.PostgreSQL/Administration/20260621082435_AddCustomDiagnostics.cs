using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddCustomDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomDiagnostics",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LongDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsChiropractic = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_CustomDiagnostics", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomDiagnostics_Code",
                schema: "administration",
                table: "CustomDiagnostics",
                columns: new[] { "Code", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_CustomDiagnostics_IsActive",
                schema: "administration",
                table: "CustomDiagnostics",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CustomDiagnostics_IsDeleted",
                schema: "administration",
                table: "CustomDiagnostics",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CustomDiagnostics_LegacyId",
                schema: "administration",
                table: "CustomDiagnostics",
                column: "LegacyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomDiagnostics",
                schema: "administration");
        }
    }
}
