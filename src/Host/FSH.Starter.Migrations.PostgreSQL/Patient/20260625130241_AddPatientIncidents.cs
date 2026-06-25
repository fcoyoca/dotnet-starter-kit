using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Patient
{
    /// <inheritdoc />
    public partial class AddPatientIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientIncidents",
                schema: "patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DateOfInitialVisit = table.Column<DateTime>(type: "date", nullable: true),
                    DateOfLoss = table.Column<DateTime>(type: "date", nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    IsTransfer = table.Column<bool>(type: "boolean", nullable: false),
                    IsAccident = table.Column<bool>(type: "boolean", nullable: false),
                    AccidentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    AccidentState = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Comments = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    SummaryOfCare = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    AdherenceToPlan = table.Column<int>(type: "integer", nullable: true),
                    PatientStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientIncidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatientIncidentDiagnostics",
                schema: "patient",
                columns: table => new
                {
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiagnosticId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientIncidentDiagnostics", x => new { x.IncidentId, x.DiagnosticId });
                    table.ForeignKey(
                        name: "FK_PatientIncidentDiagnostics_PatientIncidents_IncidentId",
                        column: x => x.IncidentId,
                        principalSchema: "patient",
                        principalTable: "PatientIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientIncidents_IsDeleted",
                schema: "patient",
                table: "PatientIncidents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PatientIncidents_PatientId",
                schema: "patient",
                table: "PatientIncidents",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientIncidents_PatientId_IsClosed",
                schema: "patient",
                table: "PatientIncidents",
                columns: new[] { "PatientId", "IsClosed" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientIncidents_PatientId_IsDeleted",
                schema: "patient",
                table: "PatientIncidents",
                columns: new[] { "PatientId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientIncidentDiagnostics",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "PatientIncidents",
                schema: "patient");
        }
    }
}
