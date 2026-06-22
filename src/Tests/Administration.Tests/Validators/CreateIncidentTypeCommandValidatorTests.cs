using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.IncidentTypes;
using FSH.Modules.Administration.Features.v1.IncidentTypes.CreateIncidentType;

namespace Administration.Tests.Validators;

public sealed class CreateIncidentTypeCommandValidatorTests
{
    private readonly CreateIncidentTypeCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(new CreateIncidentTypeCommand("Slip and Fall"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_NameIsBlank(string name)
    {
        _sut.TestValidate(new CreateIncidentTypeCommand(name))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_Should_Fail_When_NameExceedsMaxLength()
    {
        _sut.TestValidate(new CreateIncidentTypeCommand(new string('X', 201)))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }
}
