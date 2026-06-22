using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.Macros;
using FSH.Modules.Administration.Features.v1.Macros.CreateMacro;

namespace Administration.Tests.Validators;

public sealed class CreateMacroCommandValidatorTests
{
    private readonly CreateMacroCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(new CreateMacroCommand("Normal Exam", "Body text"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_NameIsBlank(string name)
    {
        _sut.TestValidate(new CreateMacroCommand(name))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_Should_Fail_When_NameExceedsMaxLength()
    {
        _sut.TestValidate(new CreateMacroCommand(new string('X', 201)))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_Should_Fail_When_TextExceedsMaxLength()
    {
        _sut.TestValidate(new CreateMacroCommand("Normal Exam", new string('X', 8001)))
            .ShouldHaveValidationErrorFor(x => x.Text);
    }
}
