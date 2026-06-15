using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Patient
{
    /// <inheritdoc />
    public partial class InitialPatient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "patient");

            migrationBuilder.CreateTable(
                name: "Patients",
                schema: "patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MiddleInitial = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    Demographics_DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    MaritalStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Demographics_IsMinor = table.Column<bool>(type: "boolean", nullable: false),
                    Demographics_RaceId = table.Column<int>(type: "integer", nullable: true),
                    Demographics_EthnicityId = table.Column<int>(type: "integer", nullable: true),
                    Demographics_LanguageId = table.Column<int>(type: "integer", nullable: true),
                    Demographics_SmokingStatusId = table.Column<int>(type: "integer", nullable: true),
                    Demographics_SmokingStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Demographics_SmokingEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MedicalAlertNotes = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Address1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Address2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    ZipCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PhoneExtension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CellPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Contact_PreferredContactMethodId = table.Column<int>(type: "integer", nullable: true),
                    SsnEncrypted = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SsnSearchHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    GuardianSsnEncrypted = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Occupation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EmployerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmployerAddress1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmployerAddress2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmployerCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EmployerState = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    EmployerZipCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    EmployerPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EmployerPhoneExtension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    GuardianFirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GuardianLastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GuardianMiddleInitial = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    Guardian_DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuardianGender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    GuardianMaritalStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    GuardianAddress1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GuardianAddress2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GuardianCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GuardianState = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    GuardianZipCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    GuardianPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    GuardianCellPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    GuardianEmployerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GuardianEmployerAddress1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GuardianEmployerAddress2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GuardianEmployerCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GuardianEmployerState = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    GuardianEmployerZipCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    NextOfKinFirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NextOfKinLastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NextOfKinPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NextOfKinRelation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NextOfKinRelationRoleCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    InsuredFullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Insurance_InsuredDateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InsuredEmployerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Insurance_ReferralTypeId = table.Column<int>(type: "integer", nullable: true),
                    HasNoKnownProblems = table.Column<bool>(type: "boolean", nullable: false),
                    HasNoKnownMedications = table.Column<bool>(type: "boolean", nullable: false),
                    HasNoKnownAllergies = table.Column<bool>(type: "boolean", nullable: false),
                    ReceivesEmailReminders = table.Column<bool>(type: "boolean", nullable: false),
                    LastVisitDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextVisitDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_IsActive",
                schema: "patient",
                table: "Patients",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_IsDeleted",
                schema: "patient",
                table: "Patients",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_LastName",
                schema: "patient",
                table: "Patients",
                column: "LastName");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PatientCode",
                schema: "patient",
                table: "Patients",
                columns: new[] { "PatientCode", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_SsnSearchHash",
                schema: "patient",
                table: "Patients",
                column: "SsnSearchHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Patients",
                schema: "patient");
        }
    }
}
