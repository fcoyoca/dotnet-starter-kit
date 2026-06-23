using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddMacroUseableByUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UseableByUserId",
                schema: "administration",
                table: "Macros",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Macros_UseableByUserId",
                schema: "administration",
                table: "Macros",
                column: "UseableByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Macros_UseableByUserId",
                schema: "administration",
                table: "Macros");

            migrationBuilder.DropColumn(
                name: "UseableByUserId",
                schema: "administration",
                table: "Macros");
        }
    }
}
