using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Scheduling
{
    /// <inheritdoc />
    public partial class AppointmentSchedulingEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAtUtc",
                schema: "scheduling",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RescheduledToAppointmentId",
                schema: "scheduling",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReservationSeriesId",
                schema: "scheduling",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ReservationSeriesId",
                schema: "scheduling",
                table: "Appointments",
                column: "ReservationSeriesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_ReservationSeriesId",
                schema: "scheduling",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ConfirmedAtUtc",
                schema: "scheduling",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RescheduledToAppointmentId",
                schema: "scheduling",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ReservationSeriesId",
                schema: "scheduling",
                table: "Appointments");
        }
    }
}
