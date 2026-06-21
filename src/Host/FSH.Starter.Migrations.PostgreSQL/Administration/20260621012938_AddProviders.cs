using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class AddProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Providers",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Suffix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Specialty = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Npi = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    KareoExternalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PrimaryClinicId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LegacyUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Providers_Clinics_PrimaryClinicId",
                        column: x => x.PrimaryClinicId,
                        principalSchema: "administration",
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_IsActive",
                schema: "administration",
                table: "Providers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_IsDeleted",
                schema: "administration",
                table: "Providers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_LegacyUserId",
                schema: "administration",
                table: "Providers",
                column: "LegacyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Npi",
                schema: "administration",
                table: "Providers",
                columns: new[] { "Npi", "TenantId" },
                unique: true,
                filter: "\"Npi\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_PrimaryClinicId",
                schema: "administration",
                table: "Providers",
                column: "PrimaryClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_UserId",
                schema: "administration",
                table: "Providers",
                columns: new[] { "UserId", "TenantId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL AND \"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Providers",
                schema: "administration");
        }
    }
}
