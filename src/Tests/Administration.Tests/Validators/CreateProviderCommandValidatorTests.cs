using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Features.v1.Providers.CreateProvider;

namespace Administration.Tests.Validators;

public sealed class CreateProviderCommandValidatorTests
{
    private readonly CreateProviderCommandValidator _sut = new();

    private static CreateProviderCommand Valid() => new(
        FirstName: "Gregory",
        LastName: "House",
        Prefix: "Dr.",
        Suffix: "MD",
        Specialty: "Diagnostics",
        Npi: "1234567890",
        KareoExternalId: "KAREO-1",
        PrimaryClinicId: null,
        UserId: null);

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
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
    public void Validate_Should_Pass_When_NpiIsNull()
    {
        _sut.TestValidate(Valid() with { Npi = null })
            .ShouldNotHaveValidationErrorFor(x => x.Npi);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345678901")]
    [InlineData("12345abcde")]
    public void Validate_Should_Fail_When_NpiNotTenDigits(string npi)
    {
        _sut.TestValidate(Valid() with { Npi = npi })
            .ShouldHaveValidationErrorFor(x => x.Npi);
    }

    [Fact]
    public void Validate_Should_Fail_When_SpecialtyExceedsMaxLength()
    {
        _sut.TestValidate(Valid() with { Specialty = new string('X', 151) })
            .ShouldHaveValidationErrorFor(x => x.Specialty);
    }
}
