using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ReferralTypes;

namespace FSH.Modules.Administration.Features.v1.ReferralTypes.CreateReferralType;

public sealed class CreateReferralTypeCommandValidator : AbstractValidator<CreateReferralTypeCommand>
{
    public CreateReferralTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
