using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Patient
{
    /// <inheritdoc />
    public partial class AddPatientLegacyUniqueId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LegacyUniqueId",
                schema: "patient",
                table: "Patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_LegacyUniqueId",
                schema: "patient",
                table: "Patients",
                column: "LegacyUniqueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_LegacyUniqueId",
                schema: "patient",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "LegacyUniqueId",
                schema: "patient",
                table: "Patients");
        }
    }
}
