using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddInsuranceCompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InsuranceCompanies",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InsuranceTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    FormularyTiers = table.Column<int>(type: "integer", nullable: false),
                    Address1 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Address2 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Zip = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_InsuranceCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceCompanies_InsuranceTypes_InsuranceTypeId",
                        column: x => x.InsuranceTypeId,
                        principalSchema: "administration",
                        principalTable: "InsuranceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCompanies_InsuranceTypeId",
                schema: "administration",
                table: "InsuranceCompanies",
                column: "InsuranceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCompanies_IsActive",
                schema: "administration",
                table: "InsuranceCompanies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCompanies_IsDeleted",
                schema: "administration",
                table: "InsuranceCompanies",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCompanies_LegacyId",
                schema: "administration",
                table: "InsuranceCompanies",
                column: "LegacyId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCompanies_Name",
                schema: "administration",
                table: "InsuranceCompanies",
                columns: new[] { "Name", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsuranceCompanies",
                schema: "administration");
        }
    }
}
