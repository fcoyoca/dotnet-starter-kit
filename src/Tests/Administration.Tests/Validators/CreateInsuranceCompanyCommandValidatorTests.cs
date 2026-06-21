using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;
using FSH.Modules.Administration.Features.v1.InsuranceCompanies.CreateInsuranceCompany;

namespace Administration.Tests.Validators;

public sealed class CreateInsuranceCompanyCommandValidatorTests
{
    private readonly CreateInsuranceCompanyCommandValidator _sut = new();

    private static CreateInsuranceCompanyCommand Valid() => new(
        Name: "Blue Shield",
        InsuranceTypeId: null,
        FormularyTiers: 3,
        Address1: "1 Market St",
        City: "San Francisco",
        State: "CA",
        Zip: "94105",
        Phone: "555-2000");

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_NameIsBlank(string name)
    {
        _sut.TestValidate(Valid() with { Name = name })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_Should_Fail_When_NameExceedsMaxLength()
    {
        _sut.TestValidate(Valid() with { Name = new string('X', 201) })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(21)]
    public void Validate_Should_Fail_When_FormularyTiersOutOfRange(int tiers)
    {
        _sut.TestValidate(Valid() with { FormularyTiers = tiers })
            .ShouldHaveValidationErrorFor(x => x.FormularyTiers);
    }

    [Fact]
    public void Validate_Should_Pass_When_OptionalAddressFieldsAreNull()
    {
        _sut.TestValidate(Valid() with { Address1 = null, City = null, State = null, Zip = null, Phone = null })
            .ShouldNotHaveAnyValidationErrors();
    }
}
