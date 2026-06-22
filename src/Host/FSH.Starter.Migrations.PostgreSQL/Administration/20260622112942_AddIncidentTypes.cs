using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddIncidentTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IncidentTypes",
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
                    table.PrimaryKey("PK_IncidentTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentTypes_IsActive",
                schema: "administration",
                table: "IncidentTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentTypes_IsDeleted",
                schema: "administration",
                table: "IncidentTypes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentTypes_LegacyId",
                schema: "administration",
                table: "IncidentTypes",
                column: "LegacyId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentTypes_Name",
                schema: "administration",
                table: "IncidentTypes",
                columns: new[] { "Name", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidentTypes",
                schema: "administration");
        }
    }
}
