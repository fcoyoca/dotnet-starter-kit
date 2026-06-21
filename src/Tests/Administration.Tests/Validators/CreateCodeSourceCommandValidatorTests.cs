using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.CodeSources;
using FSH.Modules.Administration.Features.v1.CodeSources.CreateCodeSource;

namespace Administration.Tests.Validators;

public sealed class CreateCodeSourceCommandValidatorTests
{
    private readonly CreateCodeSourceCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(new CreateCodeSourceCommand("CPT")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_NameEmpty()
    {
        _sut.TestValidate(new CreateCodeSourceCommand("")).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_Should_Fail_When_NameTooLong()
    {
        _sut.TestValidate(new CreateCodeSourceCommand(new string('x', 129)))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }
}
