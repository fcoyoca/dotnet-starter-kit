using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.ProcedureCategories;
using FSH.Modules.Administration.Features.v1.ProcedureCategories.CreateProcedureCategory;

namespace Administration.Tests.Validators;

public sealed class CreateProcedureCategoryCommandValidatorTests
{
    private readonly CreateProcedureCategoryCommandValidator _sut = new();

    private static CreateProcedureCategoryCommand Valid() => new(
        Name: "Radiology",
        Description: "Imaging procedures",
        IsImaging: true);

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_NameIsBlank(string name)
    {
        _sut.TestValidate(Valid() with { Name = name })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_Should_Fail_When_NameExceedsMaxLength()
    {
        _sut.TestValidate(Valid() with { Name = new string('X', 201) })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_Should_Pass_When_DescriptionIsNull()
    {
        _sut.TestValidate(Valid() with { Description = null })
            .ShouldNotHaveAnyValidationErrors();
    }
}
