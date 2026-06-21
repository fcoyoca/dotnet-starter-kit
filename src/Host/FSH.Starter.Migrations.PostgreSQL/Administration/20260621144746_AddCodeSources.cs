using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddCodeSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodeSources",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSources", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "administration",
                table: "CodeSources",
                columns: new[] { "Id", "DeletedBy", "DeletedOnUtc", "IsActive", "IsDeleted", "Name" },
                values: new object[,]
                {
                    { 1, null, null, true, false, "CPT" },
                    { 2, null, null, true, false, "HCPCS" },
                    { 3, null, null, true, false, "CDT" },
                    { 4, null, null, true, false, "NDC" },
                    { 5, null, null, true, false, "ICD-10-PCS" },
                    { 6, null, null, true, false, "Custom" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeSources_IsDeleted",
                schema: "administration",
                table: "CodeSources",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSources_Name",
                schema: "administration",
                table: "CodeSources",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeSources",
                schema: "administration");
        }
    }
}
