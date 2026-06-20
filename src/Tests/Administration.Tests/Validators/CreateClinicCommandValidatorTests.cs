using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.Clinics;
using FSH.Modules.Administration.Features.v1.Clinics.CreateClinic;

namespace Administration.Tests.Validators;

public sealed class CreateClinicCommandValidatorTests
{
    private readonly CreateClinicCommandValidator _sut = new();

    private static CreateClinicCommand Valid() => new(
        Code: "C-001",
        Name: "Main Clinic",
        Address1: "123 Main St",
        Address2: null,
        City: "Springfield",
        State: "IL",
        Zip: "62704",
        Phone: "555-1000");

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_CodeIsBlank(string code)
    {
        _sut.TestValidate(Valid() with { Code = code })
            .ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_Should_Fail_When_CodeExceedsMaxLength()
    {
        _sut.TestValidate(Valid() with { Code = new string('X', 51) })
            .ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_NameIsBlank(string name)
    {
        _sut.TestValidate(Valid() with { Name = name })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_Address1IsBlank(string addr)
    {
        _sut.TestValidate(Valid() with { Address1 = addr })
            .ShouldHaveValidationErrorFor(x => x.Address1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_CityIsBlank(string city)
    {
        _sut.TestValidate(Valid() with { City = city })
            .ShouldHaveValidationErrorFor(x => x.City);
    }

    [Fact]
    public void Validate_Should_Fail_When_ZipExceedsMaxLength()
    {
        _sut.TestValidate(Valid() with { Zip = new string('9', 11) })
            .ShouldHaveValidationErrorFor(x => x.Zip);
    }

    [Fact]
    public void Validate_Should_Pass_When_OptionalFieldsAreNull()
    {
        _sut.TestValidate(Valid() with { Address2 = null, Phone = null })
            .ShouldNotHaveAnyValidationErrors();
    }
}
