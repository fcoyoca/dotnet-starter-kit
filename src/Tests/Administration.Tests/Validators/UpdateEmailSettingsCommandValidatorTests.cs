using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.EmailSettings;
using FSH.Modules.Administration.Features.v1.EmailSettings.UpdateEmailSettings;

namespace Administration.Tests.Validators;

public sealed class UpdateEmailSettingsCommandValidatorTests
{
    private readonly UpdateEmailSettingsCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_When_UsingSystemServer()
    {
        _sut.TestValidate(new UpdateEmailSettingsCommand(UseCustomSmtp: false))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_For_ValidCustomServer()
    {
        _sut.TestValidate(new UpdateEmailSettingsCommand(
                UseCustomSmtp: true, Host: "smtp.example.com", Port: 587, UseSsl: true,
                Username: "mailer", Password: "secret", FromAddress: "clinic@example.com"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_CustomServer_Missing_Host()
    {
        _sut.TestValidate(new UpdateEmailSettingsCommand(UseCustomSmtp: true, Port: 587))
            .ShouldHaveValidationErrorFor(x => x.Host);
    }

    [Fact]
    public void Validate_Should_Fail_When_CustomServer_Port_OutOfRange()
    {
        _sut.TestValidate(new UpdateEmailSettingsCommand(UseCustomSmtp: true, Host: "smtp.example.com", Port: 70000))
            .ShouldHaveValidationErrorFor(x => x.Port);
    }

    [Fact]
    public void Validate_Should_Fail_For_Invalid_FromAddress()
    {
        _sut.TestValidate(new UpdateEmailSettingsCommand(UseCustomSmtp: false, FromAddress: "not-an-email"))
            .ShouldHaveValidationErrorFor(x => x.FromAddress);
    }
}
