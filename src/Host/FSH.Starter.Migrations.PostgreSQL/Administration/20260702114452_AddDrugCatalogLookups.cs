using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddDrugCatalogLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllergyReactions",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Term = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SnomedCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllergyReactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Drugs",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    RxAui = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    RxCui = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    Tty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Sab = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drugs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicationDoseUnits",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationDoseUnits", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "administration",
                table: "AllergyReactions",
                columns: new[] { "Id", "DeletedBy", "DeletedOnUtc", "IsActive", "IsDeleted", "SnomedCode", "Term" },
                values: new object[,]
                {
                    { 1, null, null, true, false, "271807003", "Rash" },
                    { 2, null, null, true, false, "126485001", "Hives" },
                    { 3, null, null, true, false, "39579001", "Anaphylaxis" },
                    { 4, null, null, true, false, "422587007", "Nausea" },
                    { 5, null, null, true, false, "422400008", "Vomiting" },
                    { 6, null, null, true, false, "65124004", "Swelling" },
                    { 7, null, null, true, false, "418290006", "Itching" },
                    { 8, null, null, true, false, "267036007", "Shortness of breath" },
                    { 9, null, null, true, false, "62315008", "Diarrhea" },
                    { 10, null, null, true, false, "49727002", "Cough" }
                });

            migrationBuilder.InsertData(
                schema: "administration",
                table: "MedicationDoseUnits",
                columns: new[] { "Id", "DeletedBy", "DeletedOnUtc", "IsActive", "IsDeleted", "Name" },
                values: new object[,]
                {
                    { 1, null, null, true, false, "mg" },
                    { 2, null, null, true, false, "mcg" },
                    { 3, null, null, true, false, "g" },
                    { 4, null, null, true, false, "mL" },
                    { 5, null, null, true, false, "tablet" },
                    { 6, null, null, true, false, "capsule" },
                    { 7, null, null, true, false, "unit" },
                    { 8, null, null, true, false, "puff" },
                    { 9, null, null, true, false, "drop" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllergyReactions_IsDeleted",
                schema: "administration",
                table: "AllergyReactions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AllergyReactions_Term",
                schema: "administration",
                table: "AllergyReactions",
                column: "Term",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Drugs_IsDeleted",
                schema: "administration",
                table: "Drugs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Drugs_Name",
                schema: "administration",
                table: "Drugs",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Drugs_RxAui",
                schema: "administration",
                table: "Drugs",
                column: "RxAui",
                unique: true,
                filter: "\"RxAui\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Drugs_RxCui",
                schema: "administration",
                table: "Drugs",
                column: "RxCui");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationDoseUnits_IsDeleted",
                schema: "administration",
                table: "MedicationDoseUnits",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationDoseUnits_Name",
                schema: "administration",
                table: "MedicationDoseUnits",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllergyReactions",
                schema: "administration");

            migrationBuilder.DropTable(
                name: "Drugs",
                schema: "administration");

            migrationBuilder.DropTable(
                name: "MedicationDoseUnits",
                schema: "administration");
        }
    }
}
