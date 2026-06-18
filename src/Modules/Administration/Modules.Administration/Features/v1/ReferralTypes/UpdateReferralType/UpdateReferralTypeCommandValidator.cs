using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ReferralTypes;

namespace FSH.Modules.Administration.Features.v1.ReferralTypes.UpdateReferralType;

public sealed class UpdateReferralTypeCommandValidator : AbstractValidator<UpdateReferralTypeCommand>
{
    public UpdateReferralTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
