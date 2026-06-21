using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class MakeProcedureCodeCategoryRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcedureCodes_ProcedureCategories_ProcedureCategoryId",
                schema: "administration",
                table: "ProcedureCodes");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProcedureCategoryId",
                schema: "administration",
                table: "ProcedureCodes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcedureCodes_ProcedureCategories_ProcedureCategoryId",
                schema: "administration",
                table: "ProcedureCodes",
                column: "ProcedureCategoryId",
                principalSchema: "administration",
                principalTable: "ProcedureCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcedureCodes_ProcedureCategories_ProcedureCategoryId",
                schema: "administration",
                table: "ProcedureCodes");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProcedureCategoryId",
                schema: "administration",
                table: "ProcedureCodes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcedureCodes_ProcedureCategories_ProcedureCategoryId",
                schema: "administration",
                table: "ProcedureCodes",
                column: "ProcedureCategoryId",
                principalSchema: "administration",
                principalTable: "ProcedureCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
