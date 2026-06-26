using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Providers;

namespace FSH.Modules.Administration.Features.v1.Providers.SetProviderSignature;

public sealed class SetProviderSignatureCommandValidator : AbstractValidator<SetProviderSignatureCommand>
{
    public SetProviderSignatureCommandValidator()
    {
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.ImageBase64).NotEmpty();
    }
}
