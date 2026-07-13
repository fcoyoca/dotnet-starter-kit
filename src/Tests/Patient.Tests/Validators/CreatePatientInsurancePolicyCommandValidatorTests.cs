using FluentValidation.Results;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;
using FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.CreatePatientInsurancePolicy;
using Shouldly;
using Xunit;

namespace Patient.Tests.Validators;

public sealed class CreatePatientInsurancePolicyCommandValidatorTests
{
    private readonly CreatePatientInsurancePolicyCommandValidator _validator = new();

    private static CreatePatientInsurancePolicyCommand Valid(
        SubscriberRelationship relationship = SubscriberRelationship.Self) =>
        new(
            PatientId: Guid.NewGuid(),
            InsuranceCompanyId: Guid.NewGuid(),
            InsuranceTypeId: null,
            Priority: InsurancePriority.Primary,
            PolicyNumber: "POL-1",
            GroupNumber: null,
            MemberId: null,
            CoPay: null,
            Deductible: null,
            EffectiveDate: null,
            ExpirationDate: null,
            SubscriberRelationship: relationship,
            SubscriberFirstName: "Jane",
            SubscriberLastName: "Doe",
            SubscriberDateOfBirth: new DateTime(1988, 5, 4, 0, 0, 0, DateTimeKind.Unspecified),
            SubscriberGender: "F",
            SubscriberSsn: null,
            SubscriberEmployerName: null,
            SubscriberAddress1: null,
            SubscriberAddress2: null,
            SubscriberCity: null,
            SubscriberState: null,
            SubscriberZipCode: null,
            Notes: null);

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        ValidationResult result = _validator.Validate(Valid());
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Insurance_Company_Should_Be_Required()
    {
        ValidationResult result = _validator.Validate(Valid() with { InsuranceCompanyId = Guid.Empty });
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreatePatientInsurancePolicyCommand.InsuranceCompanyId));
    }

    [Fact]
    public void Self_Subscriber_Should_Not_Require_Name_Or_Dob()
    {
        // When the patient is the subscriber the dashboard copies their demographics in, so a blank
        // subscriber block is legal here — the rule only bites for someone other than the patient.
        ValidationResult result = _validator.Validate(Valid() with
        {
            SubscriberFirstName = null,
            SubscriberLastName = null,
            SubscriberDateOfBirth = null
        });
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(SubscriberRelationship.Spouse)]
    [InlineData(SubscriberRelationship.Child)]
    [InlineData(SubscriberRelationship.Other)]
    public void Non_Self_Subscriber_Should_Require_Name_And_Dob(SubscriberRelationship relationship)
    {
        ValidationResult result = _validator.Validate(Valid(relationship) with
        {
            SubscriberFirstName = null,
            SubscriberLastName = null,
            SubscriberDateOfBirth = null
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(e => e.PropertyName).ShouldContain(
            nameof(CreatePatientInsurancePolicyCommand.SubscriberFirstName));
        result.Errors.Select(e => e.PropertyName).ShouldContain(
            nameof(CreatePatientInsurancePolicyCommand.SubscriberLastName));
        result.Errors.Select(e => e.PropertyName).ShouldContain(
            nameof(CreatePatientInsurancePolicyCommand.SubscriberDateOfBirth));
    }

    [Fact]
    public void Expiration_Before_Effective_Should_Fail()
    {
        ValidationResult result = _validator.Validate(Valid() with
        {
            EffectiveDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified),
            ExpirationDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreatePatientInsurancePolicyCommand.ExpirationDate));
    }

    [Fact]
    public void Negative_CoPay_Should_Fail()
    {
        ValidationResult result = _validator.Validate(Valid() with { CoPay = -1m });
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Overlong_Policy_Number_Should_Fail()
    {
        ValidationResult result = _validator.Validate(Valid() with { PolicyNumber = new string('x', 51) });
        result.IsValid.ShouldBeFalse();
    }
}
