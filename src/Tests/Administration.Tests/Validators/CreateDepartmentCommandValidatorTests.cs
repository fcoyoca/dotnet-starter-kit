using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.Departments;
using FSH.Modules.Administration.Features.v1.Departments.CreateDepartment;

namespace Administration.Tests.Validators;

public sealed class CreateDepartmentCommandValidatorTests
{
    private readonly CreateDepartmentCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(new CreateDepartmentCommand("Cardiology", 1)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_NameIsBlank(string name)
    {
        _sut.TestValidate(new CreateDepartmentCommand(name))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_Should_Fail_When_NameExceedsMaxLength()
    {
        _sut.TestValidate(new CreateDepartmentCommand(new string('X', 201)))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_Should_Fail_When_DisplayOrderNegative()
    {
        _sut.TestValidate(new CreateDepartmentCommand("Cardiology", -1))
            .ShouldHaveValidationErrorFor(x => x.DisplayOrder);
    }
}
