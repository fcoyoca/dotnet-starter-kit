using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FSH.Starter.Migrations.PostgreSQL.Administration
{
    /// <inheritdoc />
    public partial class InitialAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "administration");

            migrationBuilder.CreateTable(
                name: "Ethnicities",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ethnicities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreferredContactMethods",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferredContactMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Races",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Races", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferralTypes",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmokingStatuses",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SnomedCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmokingStatuses", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "administration",
                table: "Ethnicities",
                columns: new[] { "Id", "DeletedBy", "DeletedOnUtc", "IsActive", "IsDeleted", "Name" },
                values: new object[,]
                {
                    { 1, null, null, true, false, "Hispanic or Latino" },
                    { 2, null, null, true, false, "Not Hispanic or Latino" },
                    { 3, null, null, true, false, "Declined to specify" }
                });

            migrationBuilder.InsertData(
                schema: "administration",
                table: "Languages",
                columns: new[] { "Id", "DeletedBy", "DeletedOnUtc", "IsActive", "IsDeleted", "Name" },
                values: new object[,]
                {
                    { 1, null, null, true, false, "English" },
                    { 2, null, null, true, false, "Spanish" },
                    { 3, null, null, true, false, "Mandarin" },
                    { 4, null, null, true, false, "Cantonese" },
                    { 5, null, null, true, false, "Vietnamese" },
                    { 6, null, null, true, false, "Tagalog" },
                    { 7, null, null, true, false, "Korean" },
                    { 8, null, null, true, false, "Other" }
                });

            migrationBuilder.InsertData(
                schema: "administration",
                table: "PreferredContactMethods",
                columns: new[] { "Id", "DeletedBy", "DeletedOnUtc", "IsActive", "IsDeleted", "Name" },
                values: new object[,]
                {
                    { 1, null, null, true, false, "Phone" },
                    { 2, null, null, true, false, "Email" },
                    { 3, null, null, true, false, "Text/SMS" },
                    { 4, null, null, true, false, "Mail" },
                    { 5, null, null, true, false, "Portal message" }
                });

            migrationBuilder.InsertData(
                schema: "administration",
                table: "Races",
                columns: new[] { "Id", "DeletedBy", "DeletedOnUtc", "IsActive", "IsDeleted", "Name" },
                values: new object[,]
                {
                    { 1, null, null, true, false, "White" },
                    { 2, null, null, true, false, "Black or African American" },
                    { 3, null, null, true, false, "American Indian or Alaska Native" },
                    { 4, null, null, true, false, "Asian" },
                    { 5, null, null, true, false, "Native Hawaiian or Other Pacific Islander" },
                    { 6, null, null, true, false, "Other" },
                    { 7, null, null, true, false, "Declined to specify" }
                });

            migrationBuilder.InsertData(
                schema: "administration",
                table: "ReferralTypes",
                columns: new[] { "Id", "DeletedBy", "DeletedOnUtc", "IsActive", "IsDeleted", "Name" },
                values: new object[,]
                {
                    { 1, null, null, true, false, "Physician referral" },
                    { 2, null, null, true, false, "Self-referral" },
                    { 3, null, null, true, false, "Insurance referral" },
                    { 4, null, null, true, false, "Online/web" },
                    { 5, null, null, true, false, "Word of mouth" },
                    { 6, null, null, true, false, "Other" }
                });

            migrationBuilder.InsertData(
                schema: "administration",
                table: "SmokingStatuses",
                columns: new[] { "Id", "DeletedBy", "DeletedOnUtc", "IsActive", "IsDeleted", "Name", "SnomedCode" },
                values: new object[,]
                {
                    { 1, null, null, true, false, "Never smoker", null },
                    { 2, null, null, true, false, "Former smoker", null },
                    { 3, null, null, true, false, "Current every day smoker", null },
                    { 4, null, null, true, false, "Current some day smoker", null },
                    { 5, null, null, true, false, "Smoker, current status unknown", null },
                    { 6, null, null, true, false, "Unknown if ever smoked", null },
                    { 7, null, null, true, false, "Heavy tobacco smoker", null },
                    { 8, null, null, true, false, "Light tobacco smoker", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ethnicities_IsDeleted",
                schema: "administration",
                table: "Ethnicities",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Ethnicities_Name",
                schema: "administration",
                table: "Ethnicities",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Languages_IsDeleted",
                schema: "administration",
                table: "Languages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Languages_Name",
                schema: "administration",
                table: "Languages",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PreferredContactMethods_IsDeleted",
                schema: "administration",
                table: "PreferredContactMethods",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PreferredContactMethods_Name",
                schema: "administration",
                table: "PreferredContactMethods",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Races_IsDeleted",
                schema: "administration",
                table: "Races",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Races_Name",
                schema: "administration",
                table: "Races",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralTypes_IsDeleted",
                schema: "administration",
                table: "ReferralTypes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralTypes_Name",
                schema: "administration",
                table: "ReferralTypes",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_SmokingStatuses_IsDeleted",
                schema: "administration",
                table: "SmokingStatuses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SmokingStatuses_Name",
                schema: "administration",
                table: "SmokingStatuses",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ethnicities",
                schema: "administration");

            migrationBuilder.DropTable(
                name: "Languages",
                schema: "administration");

            migrationBuilder.DropTable(
                name: "PreferredContactMethods",
                schema: "administration");

            migrationBuilder.DropTable(
                name: "Races",
                schema: "administration");

            migrationBuilder.DropTable(
                name: "ReferralTypes",
                schema: "administration");

            migrationBuilder.DropTable(
                name: "SmokingStatuses",
                schema: "administration");
        }
    }
}
