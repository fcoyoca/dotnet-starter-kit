using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddPasswordResetEmailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordResetBody",
                schema: "administration",
                table: "TenantEmailSettings",
                type: "character varying(16000)",
                maxLength: 16000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetFooter",
                schema: "administration",
                table: "TenantEmailSettings",
                type: "character varying(16000)",
                maxLength: 16000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetSubject",
                schema: "administration",
                table: "TenantEmailSettings",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetBody",
                schema: "administration",
                table: "TenantEmailSettings");

            migrationBuilder.DropColumn(
                name: "PasswordResetFooter",
                schema: "administration",
                table: "TenantEmailSettings");

            migrationBuilder.DropColumn(
                name: "PasswordResetSubject",
                schema: "administration",
                table: "TenantEmailSettings");
        }
    }
}
