using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddInsuranceTypeProcedureCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProcedureCategoryId",
                schema: "administration",
                table: "InsuranceTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceTypes_ProcedureCategoryId",
                schema: "administration",
                table: "InsuranceTypes",
                column: "ProcedureCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceTypes_ProcedureCategories_ProcedureCategoryId",
                schema: "administration",
                table: "InsuranceTypes",
                column: "ProcedureCategoryId",
                principalSchema: "administration",
                principalTable: "ProcedureCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceTypes_ProcedureCategories_ProcedureCategoryId",
                schema: "administration",
                table: "InsuranceTypes");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceTypes_ProcedureCategoryId",
                schema: "administration",
                table: "InsuranceTypes");

            migrationBuilder.DropColumn(
                name: "ProcedureCategoryId",
                schema: "administration",
                table: "InsuranceTypes");
        }
    }
}
