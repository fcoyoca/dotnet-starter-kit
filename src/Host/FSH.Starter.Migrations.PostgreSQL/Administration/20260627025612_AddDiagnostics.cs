using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Diagnostics",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    LongDescription = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CodeSourceId = table.Column<int>(type: "integer", nullable: false),
                    IsChiropractic = table.Column<bool>(type: "boolean", nullable: false),
                    IsBillable = table.Column<bool>(type: "boolean", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LegacyId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diagnostics", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "administration",
                table: "CodeSources",
                columns: new[] { "Id", "DeletedBy", "DeletedOnUtc", "IsActive", "IsDeleted", "Name" },
                values: new object[] { 7, null, null, true, false, "ICD-10-CM" });

            migrationBuilder.CreateIndex(
                name: "IX_Diagnostics_Code",
                schema: "administration",
                table: "Diagnostics",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnostics_CodeSourceId",
                schema: "administration",
                table: "Diagnostics",
                column: "CodeSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnostics_CodeSourceId_Code",
                schema: "administration",
                table: "Diagnostics",
                columns: new[] { "CodeSourceId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnostics_IsDeleted",
                schema: "administration",
                table: "Diagnostics",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Diagnostics",
                schema: "administration");

            migrationBuilder.DeleteData(
                schema: "administration",
                table: "CodeSources",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
