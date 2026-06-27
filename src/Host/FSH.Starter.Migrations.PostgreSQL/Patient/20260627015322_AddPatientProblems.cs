using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Patient
{
    /// <inheritdoc />
    public partial class AddPatientProblems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientProblems",
                schema: "patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiagnosticId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiagnosticCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DiagnosticDescription = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DiagnosisDate = table.Column<DateTime>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsMedicalAlert = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientProblems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientProblems_IsDeleted",
                schema: "patient",
                table: "PatientProblems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PatientProblems_PatientId",
                schema: "patient",
                table: "PatientProblems",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientProblems_PatientId_IsDeleted",
                schema: "patient",
                table: "PatientProblems",
                columns: new[] { "PatientId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientProblems",
                schema: "patient");
        }
    }
}
