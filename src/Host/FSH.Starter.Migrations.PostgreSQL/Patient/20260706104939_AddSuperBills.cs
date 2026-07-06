using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Patient
{
    /// <inheritdoc />
    public partial class AddSuperBills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SuperBills",
                schema: "patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsBilled = table.Column<bool>(type: "boolean", nullable: false),
                    BilledDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuperBills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuperBillProcedures",
                schema: "patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SuperBillId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Charge = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuperBillProcedures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuperBillProcedures_SuperBills_SuperBillId",
                        column: x => x.SuperBillId,
                        principalSchema: "patient",
                        principalTable: "SuperBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuperBillProcedureDiagnostics",
                schema: "patient",
                columns: table => new
                {
                    SuperBillProcedureId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiagnosticId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuperBillProcedureDiagnostics", x => new { x.SuperBillProcedureId, x.DiagnosticId });
                    table.ForeignKey(
                        name: "FK_SuperBillProcedureDiagnostics_SuperBillProcedures_SuperBill~",
                        column: x => x.SuperBillProcedureId,
                        principalSchema: "patient",
                        principalTable: "SuperBillProcedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SuperBillProcedures_SuperBillId",
                schema: "patient",
                table: "SuperBillProcedures",
                column: "SuperBillId");

            migrationBuilder.CreateIndex(
                name: "IX_SuperBills_PatientId",
                schema: "patient",
                table: "SuperBills",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_SuperBills_ReportId",
                schema: "patient",
                table: "SuperBills",
                columns: new[] { "ReportId", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SuperBillProcedureDiagnostics",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "SuperBillProcedures",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "SuperBills",
                schema: "patient");
        }
    }
}
