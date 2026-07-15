using FluentValidation;
using FSH.Modules.Claims.Contracts.v1.Claims;

namespace FSH.Modules.Claims.Features.v1.Claims.MarkPaid;

public sealed class MarkClaimPaidCommandValidator : AbstractValidator<MarkClaimPaidCommand>
{
    public MarkClaimPaidCommandValidator() => RuleFor(x => x.ClaimId).NotEmpty();
}
