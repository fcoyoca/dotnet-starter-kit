using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;
using FSH.Modules.Administration.Features.v1.PatientDocumentTypes.CreatePatientDocumentType;

namespace Administration.Tests.Validators;

public sealed class CreatePatientDocumentTypeCommandValidatorTests
{
    private readonly CreatePatientDocumentTypeCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(new CreatePatientDocumentTypeCommand("Lab Result"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_NameIsBlank(string name)
    {
        _sut.TestValidate(new CreatePatientDocumentTypeCommand(name))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_Should_Fail_When_NameExceedsMaxLength()
    {
        _sut.TestValidate(new CreatePatientDocumentTypeCommand(new string('X', 201)))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }
}
