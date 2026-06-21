using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddInsuranceTypeProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InsuranceTypeProcedures",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InsuranceTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LegacyId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceTypeProcedures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceTypeProcedures_InsuranceTypes_InsuranceTypeId",
                        column: x => x.InsuranceTypeId,
                        principalSchema: "administration",
                        principalTable: "InsuranceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InsuranceTypeProcedures_ProcedureCodes_ProcedureCodeId",
                        column: x => x.ProcedureCodeId,
                        principalSchema: "administration",
                        principalTable: "ProcedureCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceTypeProcedures_InsuranceTypeId",
                schema: "administration",
                table: "InsuranceTypeProcedures",
                column: "InsuranceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceTypeProcedures_InsuranceTypeId_ProcedureCodeId",
                schema: "administration",
                table: "InsuranceTypeProcedures",
                columns: new[] { "InsuranceTypeId", "ProcedureCodeId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceTypeProcedures_LegacyId",
                schema: "administration",
                table: "InsuranceTypeProcedures",
                column: "LegacyId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceTypeProcedures_ProcedureCodeId",
                schema: "administration",
                table: "InsuranceTypeProcedures",
                column: "ProcedureCodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsuranceTypeProcedures",
                schema: "administration");
        }
    }
}
