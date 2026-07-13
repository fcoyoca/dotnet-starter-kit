using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Patient
{
    /// <inheritdoc />
    public partial class AddPatientInsurancePolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The three Insured* columns below belonged to the old owned PatientInsurance value
            // object, which held a subscriber but no payer, policy number or priority. They are
            // dropped rather than back-filled into PatientInsurancePolicies: a policy requires an
            // InsuranceCompanyId, and the value object carried none, so no valid policy row can be
            // synthesized from them. Nothing is lost — the real legacy insurance records live in
            // BackChart's dbo.PatientInsurance (many per patient) and import into the new table.
            // Insurance_ReferralTypeId is a patient-level referral source, not insurance, and is
            // renamed onto the Patients root below, preserving its data.
            migrationBuilder.DropColumn(
                name: "Insurance_InsuredDateOfBirth",
                schema: "patient",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "InsuredEmployerName",
                schema: "patient",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "InsuredFullName",
                schema: "patient",
                table: "Patients");

            migrationBuilder.RenameColumn(
                name: "Insurance_ReferralTypeId",
                schema: "patient",
                table: "Patients",
                newName: "ReferralTypeId");

            migrationBuilder.CreateTable(
                name: "PatientInsurancePolicies",
                schema: "patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    InsuranceCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InsuranceTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PolicyNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GroupNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MemberId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CoPay = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Deductible = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "date", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "date", nullable: true),
                    SubscriberRelationship = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SubscriberFirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubscriberLastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubscriberDateOfBirth = table.Column<DateTime>(type: "date", nullable: true),
                    SubscriberGender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SubscriberSsnEncrypted = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SubscriberEmployerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SubscriberAddress1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SubscriberAddress2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SubscriberCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubscriberState = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    SubscriberZipCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientInsurancePolicies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientInsurancePolicies_PatientId",
                schema: "patient",
                table: "PatientInsurancePolicies",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientInsurancePolicies_PatientId_IsActive",
                schema: "patient",
                table: "PatientInsurancePolicies",
                columns: new[] { "PatientId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientInsurancePolicies_PatientId_Priority",
                schema: "patient",
                table: "PatientInsurancePolicies",
                columns: new[] { "PatientId", "Priority", "TenantId" },
                unique: true,
                filter: "\"IsActive\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientInsurancePolicies",
                schema: "patient");

            migrationBuilder.RenameColumn(
                name: "ReferralTypeId",
                schema: "patient",
                table: "Patients",
                newName: "Insurance_ReferralTypeId");

            migrationBuilder.AddColumn<DateTime>(
                name: "Insurance_InsuredDateOfBirth",
                schema: "patient",
                table: "Patients",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuredEmployerName",
                schema: "patient",
                table: "Patients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuredFullName",
                schema: "patient",
                table: "Patients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
