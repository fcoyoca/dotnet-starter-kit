using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Scheduling
{
    /// <inheritdoc />
    public partial class AddAppointmentReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReservation",
                schema: "scheduling",
                table: "Appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReservationTitle",
                schema: "scheduling",
                table: "Appointments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReservation",
                schema: "scheduling",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ReservationTitle",
                schema: "scheduling",
                table: "Appointments");
        }
    }
}
