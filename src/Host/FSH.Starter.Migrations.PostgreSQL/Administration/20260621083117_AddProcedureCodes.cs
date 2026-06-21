using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddProcedureCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcedureCodes",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProcedureCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CodeSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MacroText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LegacyId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcedureCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcedureCodes_ProcedureCategories_ProcedureCategoryId",
                        column: x => x.ProcedureCategoryId,
                        principalSchema: "administration",
                        principalTable: "ProcedureCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcedureCodes_Code",
                schema: "administration",
                table: "ProcedureCodes",
                columns: new[] { "Code", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ProcedureCodes_IsActive",
                schema: "administration",
                table: "ProcedureCodes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProcedureCodes_IsDeleted",
                schema: "administration",
                table: "ProcedureCodes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProcedureCodes_LegacyId",
                schema: "administration",
                table: "ProcedureCodes",
                column: "LegacyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcedureCodes_ProcedureCategoryId",
                schema: "administration",
                table: "ProcedureCodes",
                column: "ProcedureCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcedureCodes",
                schema: "administration");
        }
    }
}
