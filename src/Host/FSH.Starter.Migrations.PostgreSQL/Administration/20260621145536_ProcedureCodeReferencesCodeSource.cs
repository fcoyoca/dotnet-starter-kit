using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class ProcedureCodeReferencesCodeSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeSource",
                schema: "administration",
                table: "ProcedureCodes");

            migrationBuilder.AddColumn<int>(
                name: "CodeSourceId",
                schema: "administration",
                table: "ProcedureCodes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcedureCodes_CodeSourceId",
                schema: "administration",
                table: "ProcedureCodes",
                column: "CodeSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcedureCodes_CodeSources_CodeSourceId",
                schema: "administration",
                table: "ProcedureCodes",
                column: "CodeSourceId",
                principalSchema: "administration",
                principalTable: "CodeSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcedureCodes_CodeSources_CodeSourceId",
                schema: "administration",
                table: "ProcedureCodes");

            migrationBuilder.DropIndex(
                name: "IX_ProcedureCodes_CodeSourceId",
                schema: "administration",
                table: "ProcedureCodes");

            migrationBuilder.DropColumn(
                name: "CodeSourceId",
                schema: "administration",
                table: "ProcedureCodes");

            migrationBuilder.AddColumn<string>(
                name: "CodeSource",
                schema: "administration",
                table: "ProcedureCodes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
