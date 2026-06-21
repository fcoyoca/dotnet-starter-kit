using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;
using FSH.Modules.Administration.Features.v1.DiagnosticCategories.CreateDiagnosticCategory;

namespace Administration.Tests.Validators;

public sealed class CreateDiagnosticCategoryCommandValidatorTests
{
    private readonly CreateDiagnosticCategoryCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(new CreateDiagnosticCategoryCommand("Cardiology"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_NameIsBlank(string name)
    {
        _sut.TestValidate(new CreateDiagnosticCategoryCommand(name))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_Should_Fail_When_NameExceedsMaxLength()
    {
        _sut.TestValidate(new CreateDiagnosticCategoryCommand(new string('X', 201)))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }
}
