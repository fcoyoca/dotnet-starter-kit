using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ReferralTypes;

namespace FSH.Modules.Administration.Features.v1.ReferralTypes.DeleteReferralType;

public sealed class DeleteReferralTypeCommandValidator : AbstractValidator<DeleteReferralTypeCommand>
{
    public DeleteReferralTypeCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
