using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using FSH.Modules.Administration.Features.v1.CustomDiagnostics.EnsureCustomDiagnostic;

namespace Administration.Tests.Validators;

public sealed class EnsureCustomDiagnosticCommandValidatorTests
{
    private readonly EnsureCustomDiagnosticCommandValidator _sut = new();

    private static EnsureCustomDiagnosticCommand Valid() => new(
        Code: "M54.5",
        Description: "Low back pain",
        LongDescription: "Chronic low back pain",
        IsChiropractic: true);

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

    [Fact]
    public void Validate_Should_Fail_When_DescriptionExceedsMaxLength()
    {
        _sut.TestValidate(Valid() with { Description = new string('X', 501) })
            .ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_Should_Fail_When_LongDescriptionExceedsMaxLength()
    {
        _sut.TestValidate(Valid() with { LongDescription = new string('X', 2001) })
            .ShouldHaveValidationErrorFor(x => x.LongDescription);
    }

    [Fact]
    public void Validate_Should_Pass_When_OptionalFieldsAreNull()
    {
        _sut.TestValidate(Valid() with { Description = null, LongDescription = null })
            .ShouldNotHaveAnyValidationErrors();
    }
}
