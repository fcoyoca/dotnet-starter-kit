using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Features.v1.Providers.SetProviderSignature;

namespace Administration.Tests.Validators;

public sealed class SetProviderSignatureCommandValidatorTests
{
    private readonly SetProviderSignatureCommandValidator _sut = new();

    private static SetProviderSignatureCommand Valid() =>
        new(Guid.CreateVersion7(), "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_ProviderIdEmpty()
    {
        _sut.TestValidate(Valid() with { ProviderId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.ProviderId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_ImageBase64Blank(string image)
    {
        _sut.TestValidate(Valid() with { ImageBase64 = image })
            .ShouldHaveValidationErrorFor(x => x.ImageBase64);
    }
}
