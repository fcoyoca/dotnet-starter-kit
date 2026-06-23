using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddReportTemplateCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Macros_Name",
                schema: "administration",
                table: "Macros");

            migrationBuilder.AddColumn<int>(
                name: "ReportFieldId",
                schema: "administration",
                table: "Macros",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReportTypes",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LegacyId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportFields",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReportTypeId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LegacyId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportFields_ReportTypes_ReportTypeId",
                        column: x => x.ReportTypeId,
                        principalSchema: "administration",
                        principalTable: "ReportTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Macros_ReportFieldId",
                schema: "administration",
                table: "Macros",
                column: "ReportFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Macros_ReportFieldId_Name",
                schema: "administration",
                table: "Macros",
                columns: new[] { "ReportFieldId", "Name", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ReportFields_IsDeleted",
                schema: "administration",
                table: "ReportFields",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ReportFields_LegacyId",
                schema: "administration",
                table: "ReportFields",
                column: "LegacyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportFields_ReportTypeId",
                schema: "administration",
                table: "ReportFields",
                column: "ReportTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportFields_ReportTypeId_Name",
                schema: "administration",
                table: "ReportFields",
                columns: new[] { "ReportTypeId", "Name", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ReportTypes_IsDeleted",
                schema: "administration",
                table: "ReportTypes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ReportTypes_LegacyId",
                schema: "administration",
                table: "ReportTypes",
                column: "LegacyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportTypes_Name",
                schema: "administration",
                table: "ReportTypes",
                columns: new[] { "Name", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.AddForeignKey(
                name: "FK_Macros_ReportFields_ReportFieldId",
                schema: "administration",
                table: "Macros",
                column: "ReportFieldId",
                principalSchema: "administration",
                principalTable: "ReportFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Macros_ReportFields_ReportFieldId",
                schema: "administration",
                table: "Macros");

            migrationBuilder.DropTable(
                name: "ReportFields",
                schema: "administration");

            migrationBuilder.DropTable(
                name: "ReportTypes",
                schema: "administration");

            migrationBuilder.DropIndex(
                name: "IX_Macros_ReportFieldId",
                schema: "administration",
                table: "Macros");

            migrationBuilder.DropIndex(
                name: "IX_Macros_ReportFieldId_Name",
                schema: "administration",
                table: "Macros");

            migrationBuilder.DropColumn(
                name: "ReportFieldId",
                schema: "administration",
                table: "Macros");

            migrationBuilder.CreateIndex(
                name: "IX_Macros_Name",
                schema: "administration",
                table: "Macros",
                columns: new[] { "Name", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }
    }
}
