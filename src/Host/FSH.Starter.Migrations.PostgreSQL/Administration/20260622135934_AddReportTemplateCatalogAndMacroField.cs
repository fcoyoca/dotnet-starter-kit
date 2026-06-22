using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddReportTemplateCatalogAndMacroField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Macros_Name",
                schema: "administration",
                table: "Macros");

            migrationBuilder.AddColumn<int>(
                name: "ReportFieldId",
                schema: "administration",
                table: "Macros",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReportFields",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportFields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportTypeFields",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ReportTypeId = table.Column<int>(type: "integer", nullable: false),
                    ReportFieldId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTypeFields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportTypes",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "administration",
                table: "ReportFields",
                columns: new[] { "Id", "Category", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "Chief Complaint", 1, true, "Chief Complaint" },
                    { 2, "Present Problem", 1, true, "Present Problem" },
                    { 3, "Medical History", 1, true, "Medical History" },
                    { 4, "Personal / Social History", 1, true, "Personal / Social History" },
                    { 5, "Allergies", 1, true, "Allergies" },
                    { 6, "Medications", 1, true, "Medications" },
                    { 7, "Systems Review", 1, true, "Systems Review" },
                    { 13, "Clinical Exam", 7, true, "Comments" },
                    { 14, "Diagnostic Imaging", 1, true, "Diagnostic Imaging" },
                    { 15, "Clinical Impression", 1, true, "Clinical Impression" },
                    { 16, "Therapeutic Care", 1, true, "Therapeutic Care" },
                    { 17, "Subjective", 1, false, "ADL" },
                    { 18, "Subjective", 2, false, "Pain" },
                    { 19, "Subjective", 3, true, "Subjective" },
                    { 20, "Objective", 1, true, "Objective" },
                    { 21, "Assessment", 1, false, "Assessment" },
                    { 22, "Assessment", 2, true, "Assessment" },
                    { 24, "Plan", 1, true, "Plan" },
                    { 27, "Goals", 1, true, "Short Term Goals" },
                    { 28, "Goals", 2, true, "Long Term Goals" },
                    { 29, "Work Status or Restrictions", 1, true, "Work Status or Restrictions" },
                    { 30, "Family History", 1, true, "Family History" },
                    { 31, "Assessment", 3, false, "Niall Radio Group" },
                    { 33, "Assessment", 5, false, "Niall's yes/no" },
                    { 34, "Assessment", 6, false, "Niall's drop down" },
                    { 38, "Comments", 1, true, "Comments" },
                    { 39, "Documentation", 1, true, "Documentation" }
                });

            migrationBuilder.InsertData(
                schema: "administration",
                table: "ReportTypeFields",
                columns: new[] { "Id", "ReportFieldId", "ReportTypeId" },
                values: new object[,]
                {
                    { 1, 1, 1 },
                    { 2, 2, 1 },
                    { 3, 3, 1 },
                    { 4, 4, 1 },
                    { 5, 5, 1 },
                    { 6, 6, 1 },
                    { 7, 7, 1 },
                    { 8, 14, 1 },
                    { 9, 15, 1 },
                    { 10, 24, 1 },
                    { 11, 27, 1 },
                    { 12, 29, 1 },
                    { 13, 30, 1 },
                    { 14, 28, 1 },
                    { 15, 13, 1 },
                    { 16, 1, 2 },
                    { 17, 2, 2 },
                    { 18, 3, 2 },
                    { 19, 4, 2 },
                    { 20, 5, 2 },
                    { 21, 6, 2 },
                    { 22, 7, 2 },
                    { 23, 14, 2 },
                    { 24, 15, 2 },
                    { 25, 24, 2 },
                    { 26, 27, 2 },
                    { 27, 29, 2 },
                    { 28, 30, 2 },
                    { 29, 28, 2 },
                    { 30, 13, 2 },
                    { 31, 1, 3 },
                    { 32, 2, 3 },
                    { 33, 3, 3 },
                    { 34, 4, 3 },
                    { 35, 5, 3 },
                    { 36, 6, 3 },
                    { 37, 7, 3 },
                    { 38, 14, 3 },
                    { 39, 15, 3 },
                    { 40, 24, 3 },
                    { 41, 27, 3 },
                    { 42, 29, 3 },
                    { 43, 30, 3 },
                    { 44, 28, 3 },
                    { 45, 13, 3 },
                    { 46, 17, 4 },
                    { 47, 20, 4 },
                    { 48, 21, 4 },
                    { 49, 24, 4 },
                    { 50, 18, 4 },
                    { 51, 22, 4 },
                    { 52, 19, 4 },
                    { 53, 31, 4 },
                    { 54, 33, 4 },
                    { 55, 34, 4 },
                    { 56, 38, 5 },
                    { 57, 39, 6 }
                });

            migrationBuilder.InsertData(
                schema: "administration",
                table: "ReportTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Initial Evaluation" },
                    { 2, "Progress Report" },
                    { 3, "Discharge Report" },
                    { 4, "Daily Visit" },
                    { 5, "No Show" },
                    { 6, "NoFieldReport" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Macros_ReportFieldId",
                schema: "administration",
                table: "Macros",
                column: "ReportFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Macros_ReportFieldId_Name",
                schema: "administration",
                table: "Macros",
                columns: new[] { "ReportFieldId", "Name", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ReportFields_IsActive",
                schema: "administration",
                table: "ReportFields",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ReportTypeFields_ReportTypeId",
                schema: "administration",
                table: "ReportTypeFields",
                column: "ReportTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportTypeFields_ReportTypeId_ReportFieldId",
                schema: "administration",
                table: "ReportTypeFields",
                columns: new[] { "ReportTypeId", "ReportFieldId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Macros_ReportFields_ReportFieldId",
                schema: "administration",
                table: "Macros",
                column: "ReportFieldId",
                principalSchema: "administration",
                principalTable: "ReportFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Macros_ReportFields_ReportFieldId",
                schema: "administration",
                table: "Macros");

            migrationBuilder.DropTable(
                name: "ReportFields",
                schema: "administration");

            migrationBuilder.DropTable(
                name: "ReportTypeFields",
                schema: "administration");

            migrationBuilder.DropTable(
                name: "ReportTypes",
                schema: "administration");

            migrationBuilder.DropIndex(
                name: "IX_Macros_ReportFieldId",
                schema: "administration",
                table: "Macros");

            migrationBuilder.DropIndex(
                name: "IX_Macros_ReportFieldId_Name",
                schema: "administration",
                table: "Macros");

            migrationBuilder.DropColumn(
                name: "ReportFieldId",
                schema: "administration",
                table: "Macros");

            migrationBuilder.CreateIndex(
                name: "IX_Macros_Name",
                schema: "administration",
                table: "Macros",
                columns: new[] { "Name", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }
    }
}
