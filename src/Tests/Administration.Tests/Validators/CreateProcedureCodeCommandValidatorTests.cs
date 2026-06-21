using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.ProcedureCodes;
using FSH.Modules.Administration.Features.v1.ProcedureCodes.CreateProcedureCode;

namespace Administration.Tests.Validators;

public sealed class CreateProcedureCodeCommandValidatorTests
{
    private readonly CreateProcedureCodeCommandValidator _sut = new();

    private static CreateProcedureCodeCommand Valid() => new(
        Code: "99213",
        Name: "Office visit",
        Description: "Established patient",
        ProcedureCategoryId: null,
        CodeSource: "CPT",
        MacroText: "Note macro");

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
        _sut.TestValidate(Valid() with { Name = null, Description = null, CodeSource = null, MacroText = null })
            .ShouldNotHaveAnyValidationErrors();
    }
}
