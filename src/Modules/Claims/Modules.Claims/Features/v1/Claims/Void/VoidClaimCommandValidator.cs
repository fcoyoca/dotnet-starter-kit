using FluentValidation;
using FSH.Modules.Claims.Contracts.v1.Claims;

namespace FSH.Modules.Claims.Features.v1.Claims.Void;

public sealed class VoidClaimCommandValidator : AbstractValidator<VoidClaimCommand>
{
    public VoidClaimCommandValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}
