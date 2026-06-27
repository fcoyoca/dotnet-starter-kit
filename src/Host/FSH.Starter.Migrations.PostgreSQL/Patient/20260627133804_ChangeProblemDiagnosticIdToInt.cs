using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Patient
{
    /// <inheritdoc />
    public partial class ChangeProblemDiagnosticIdToInt : Migration
    {
        // PatientProblem.DiagnosticId now references the global ICD Diagnostic catalog (int id, legacy
        // ldxID) instead of a custom-diagnostic Guid. uuid -> integer has no valid cast, so the column is
        // dropped and re-added (the table carries no production data — these migrations are unapplied).
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiagnosticId",
                schema: "patient",
                table: "PatientProblems");

            migrationBuilder.AddColumn<int>(
                name: "DiagnosticId",
                schema: "patient",
                table: "PatientProblems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiagnosticId",
                schema: "patient",
                table: "PatientProblems");

            migrationBuilder.AddColumn<Guid>(
                name: "DiagnosticId",
                schema: "patient",
                table: "PatientProblems",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);
        }
    }
}
