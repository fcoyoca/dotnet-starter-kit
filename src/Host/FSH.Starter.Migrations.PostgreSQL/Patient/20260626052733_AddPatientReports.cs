using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Patient
{
    /// <inheritdoc />
    public partial class AddPatientReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientReports",
                schema: "patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportTypeId = table.Column<int>(type: "integer", nullable: false),
                    ReportDate = table.Column<DateTime>(type: "date", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsNoShow = table.Column<bool>(type: "boolean", nullable: false),
                    VitalsHeightInches = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    VitalsWeightLbs = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    VitalsBmi = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    VitalsSystolic = table.Column<int>(type: "integer", nullable: true),
                    VitalsDiastolic = table.Column<int>(type: "integer", nullable: true),
                    VitalsPulse = table.Column<int>(type: "integer", nullable: true),
                    VitalsTemperatureF = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    WorkflowStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsSigned = table.Column<bool>(type: "boolean", nullable: false),
                    SignedByUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SignedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SignedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SignatureImagePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ReviewRequestedByUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReviewRequestedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewerProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewSignedByUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReviewSignedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReviewSignedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewSignatureImagePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatientReportAddendums",
                schema: "patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientReportAddendums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientReportAddendums_PatientReports_ReportId",
                        column: x => x.ReportId,
                        principalSchema: "patient",
                        principalTable: "PatientReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientReportFieldValues",
                schema: "patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportFieldId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientReportFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientReportFieldValues_PatientReports_ReportId",
                        column: x => x.ReportId,
                        principalSchema: "patient",
                        principalTable: "PatientReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientReportAddendums_ReportId",
                schema: "patient",
                table: "PatientReportAddendums",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReportFieldValues_ReportId_ReportFieldId",
                schema: "patient",
                table: "PatientReportFieldValues",
                columns: new[] { "ReportId", "ReportFieldId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientReports_IncidentId",
                schema: "patient",
                table: "PatientReports",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReports_IncidentId_IsDeleted",
                schema: "patient",
                table: "PatientReports",
                columns: new[] { "IncidentId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientReports_IsDeleted",
                schema: "patient",
                table: "PatientReports",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PatientReports_PatientId",
                schema: "patient",
                table: "PatientReports",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientReportAddendums",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "PatientReportFieldValues",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "PatientReports",
                schema: "patient");
        }
    }
}
