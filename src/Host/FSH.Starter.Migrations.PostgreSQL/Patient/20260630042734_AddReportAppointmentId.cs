using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Patient
{
    /// <inheritdoc />
    public partial class AddReportAppointmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId",
                schema: "patient",
                table: "PatientReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientReports_AppointmentId",
                schema: "patient",
                table: "PatientReports",
                column: "AppointmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PatientReports_AppointmentId",
                schema: "patient",
                table: "PatientReports");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                schema: "patient",
                table: "PatientReports");
        }
    }
}
