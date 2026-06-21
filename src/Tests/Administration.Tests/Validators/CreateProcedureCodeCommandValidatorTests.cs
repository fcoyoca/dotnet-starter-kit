using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.ProcedureCodes;
using FSH.Modules.Administration.Features.v1.ProcedureCodes.CreateProcedureCode;

namespace Administration.Tests.Validators;

public sealed class CreateProcedureCodeCommandValidatorTests
{
    private readonly CreateProcedureCodeCommandValidator _sut = new();

    private static CreateProcedureCodeCommand Valid() => new(
        Code: "99213",
        ProcedureCategoryId: Guid.CreateVersion7(),
        Name: "Office visit",
        Description: "Established patient",
        CodeSourceId: 1,
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
    public void Validate_Should_Fail_When_ProcedureCategoryIdEmpty()
    {
        _sut.TestValidate(Valid() with { ProcedureCategoryId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.ProcedureCategoryId);
    }

    [Fact]
    public void Validate_Should_Pass_When_OptionalFieldsAreNull()
    {
        _sut.TestValidate(Valid() with { Name = null, Description = null, CodeSourceId = null, MacroText = null })
            .ShouldNotHaveAnyValidationErrors();
    }
}
