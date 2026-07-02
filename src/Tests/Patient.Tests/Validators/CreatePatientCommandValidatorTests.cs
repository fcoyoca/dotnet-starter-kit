using FluentValidation.TestHelper;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Features.v1.Patients.CreatePatient;

namespace Patient.Tests.Validators;

public sealed class CreatePatientCommandValidatorTests
{
    private readonly CreatePatientCommandValidator _sut = new();

    private static CreatePatientCommand Valid() => new(
        PatientCode: "P-001",
        IsActive: true,
        FirstName: "John",
        LastName: "Doe",
        MiddleInitial: null,
        DateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Gender: "M",
        MaritalStatus: null,
        IsMinor: false,
        RaceId: null, EthnicityId: null, LanguageId: null,
        SmokingStatusId: null, SmokingStartDate: null, SmokingEndDate: null,
        MedicalAlertNotes: null,
        Address1: null, Address2: null, City: null, State: null, ZipCode: null,
        Phone: null, PhoneExtension: null, CellPhone: null,
        Email: null, PreferredContactMethodId: null,
        Ssn: null, GuardianSsn: null,
        Occupation: null, EmployerName: null,
        EmployerAddress1: null, EmployerAddress2: null,
        EmployerCity: null, EmployerState: null, EmployerZipCode: null,
        EmployerPhone: null, EmployerPhoneExtension: null,
        GuardianFirstName: null, GuardianLastName: null, GuardianMiddleInitial: null,
        GuardianDateOfBirth: null, GuardianGender: null, GuardianMaritalStatus: null,
        GuardianAddress1: null, GuardianAddress2: null,
        GuardianCity: null, GuardianState: null, GuardianZipCode: null,
        GuardianPhone: null, GuardianCellPhone: null,
        GuardianEmployerName: null, GuardianEmployerAddress1: null, GuardianEmployerAddress2: null,
        GuardianEmployerCity: null, GuardianEmployerState: null, GuardianEmployerZipCode: null,
        NextOfKinFirstName: null, NextOfKinLastName: null, NextOfKinPhone: null,
        NextOfKinRelation: null, NextOfKinRelationRoleCode: null,
        InsuredFullName: null, InsuredDateOfBirth: null, InsuredEmployerName: null, ReferralTypeId: null,
        HasNoKnownProblems: false, HasNoKnownMedications: false, HasNoKnownAllergies: false,
        ReceivesEmailReminders: false,
        LastVisitDate: null, NextVisitDate: null);

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_When_PatientCodeIsNull()
    {
        _sut.TestValidate(Valid() with { PatientCode = null })
            .ShouldNotHaveValidationErrorFor(x => x.PatientCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Pass_When_PatientCodeIsBlank(string code)
    {
        _sut.TestValidate(Valid() with { PatientCode = code })
            .ShouldNotHaveValidationErrorFor(x => x.PatientCode);
    }

    [Fact]
    public void Validate_Should_Fail_When_PatientCodeExceedsMaxLength()
    {
        _sut.TestValidate(Valid() with { PatientCode = new string('X', 51) })
            .ShouldHaveValidationErrorFor(x => x.PatientCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_FirstNameIsBlank(string name)
    {
        _sut.TestValidate(Valid() with { FirstName = name })
            .ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_LastNameIsBlank(string name)
    {
        _sut.TestValidate(Valid() with { LastName = name })
            .ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Validate_Should_Fail_When_DateOfBirthIsDefault()
    {
        _sut.TestValidate(Valid() with { DateOfBirth = default })
            .ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public void Validate_Should_Fail_When_DateOfBirthIsInFuture()
    {
        _sut.TestValidate(Valid() with { DateOfBirth = DateTime.UtcNow.AddDays(1) })
            .ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public void Validate_Should_Fail_When_EmailIsInvalid()
    {
        _sut.TestValidate(Valid() with { Email = "not-an-email" })
            .ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_Should_Pass_When_EmailIsNull()
    {
        _sut.TestValidate(Valid() with { Email = null })
            .ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_Should_Fail_When_MinorHasNoGuardianName()
    {
        var cmd = Valid() with
        {
            IsMinor = true,
            GuardianFirstName = null,
            GuardianLastName = null
        };

        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.GuardianFirstName);
        result.ShouldHaveValidationErrorFor(x => x.GuardianLastName);
    }

    [Fact]
    public void Validate_Should_Pass_When_MinorHasGuardianName()
    {
        var cmd = Valid() with
        {
            IsMinor = true,
            GuardianFirstName = "Mary",
            GuardianLastName = "Doe"
        };

        _sut.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_When_SsnIsNull()
    {
        _sut.TestValidate(Valid() with { Ssn = null })
            .ShouldNotHaveValidationErrorFor(x => x.Ssn);
    }

    [Fact]
    public void Validate_Should_Fail_When_SsnExceedsMaxLength()
    {
        _sut.TestValidate(Valid() with { Ssn = "123-45-67890-EXTRA" })
            .ShouldHaveValidationErrorFor(x => x.Ssn);
    }
}
