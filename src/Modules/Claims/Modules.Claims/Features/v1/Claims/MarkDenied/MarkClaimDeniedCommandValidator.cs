using FluentValidation;
using FSH.Modules.Claims.Contracts.v1.Claims;

namespace FSH.Modules.Claims.Features.v1.Claims.MarkDenied;

public sealed class MarkClaimDeniedCommandValidator : AbstractValidator<MarkClaimDeniedCommand>
{
    public MarkClaimDeniedCommandValidator() => RuleFor(x => x.ClaimId).NotEmpty();
}
