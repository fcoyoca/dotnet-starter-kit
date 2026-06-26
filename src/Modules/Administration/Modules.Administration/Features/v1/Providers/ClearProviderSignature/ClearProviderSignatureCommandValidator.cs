using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Providers;

namespace FSH.Modules.Administration.Features.v1.Providers.ClearProviderSignature;

public sealed class ClearProviderSignatureCommandValidator : AbstractValidator<ClearProviderSignatureCommand>
{
    public ClearProviderSignatureCommandValidator()
    {
        RuleFor(x => x.ProviderId).NotEmpty();
    }
}
