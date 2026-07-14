using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddClinicPrintOrientation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrintOrientation",
                schema: "administration",
                table: "Clinics",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Portrait");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintOrientation",
                schema: "administration",
                table: "Clinics");
        }
    }
}
