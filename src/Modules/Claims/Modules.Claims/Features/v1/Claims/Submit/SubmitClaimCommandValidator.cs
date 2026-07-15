using FluentValidation;
using FSH.Modules.Claims.Contracts.v1.Claims;

namespace FSH.Modules.Claims.Features.v1.Claims.Submit;

public sealed class SubmitClaimCommandValidator : AbstractValidator<SubmitClaimCommand>
{
    public SubmitClaimCommandValidator() => RuleFor(x => x.ClaimId).NotEmpty();
}
