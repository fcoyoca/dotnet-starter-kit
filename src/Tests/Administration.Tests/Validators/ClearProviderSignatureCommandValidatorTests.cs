using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Features.v1.Providers.ClearProviderSignature;

namespace Administration.Tests.Validators;

public sealed class ClearProviderSignatureCommandValidatorTests
{
    private readonly ClearProviderSignatureCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(new ClearProviderSignatureCommand(Guid.CreateVersion7()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_ProviderIdEmpty()
    {
        _sut.TestValidate(new ClearProviderSignatureCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.ProviderId);
    }
}
