using FluentValidation;
using FSH.Modules.Claims.Contracts.v1.Claims;

namespace FSH.Modules.Claims.Features.v1.Claims.MarkReady;

public sealed class MarkClaimReadyCommandValidator : AbstractValidator<MarkClaimReadyCommand>
{
    public MarkClaimReadyCommandValidator() =>
        RuleFor(x => x.ClaimId).NotEmpty();
}
