using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddReportFieldDefaultText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultText",
                schema: "administration",
                table: "ReportFields",
                type: "character varying(16000)",
                maxLength: 16000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultText",
                schema: "administration",
                table: "ReportFields");
        }
    }
}
