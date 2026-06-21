using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using FSH.Modules.Administration.Features.v1.CustomDiagnostics.CreateCustomDiagnostic;

namespace Administration.Tests.Validators;

public sealed class CreateCustomDiagnosticCommandValidatorTests
{
    private readonly CreateCustomDiagnosticCommandValidator _sut = new();

    private static CreateCustomDiagnosticCommand Valid() => new(
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
    public void Validate_Should_Pass_When_OptionalFieldsAreNull()
    {
        _sut.TestValidate(Valid() with { Description = null, LongDescription = null })
            .ShouldNotHaveAnyValidationErrors();
    }
}
