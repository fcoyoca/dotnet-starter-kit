using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;

namespace FSH.Modules.Administration.Features.v1.PreferredContactMethods.UpdatePreferredContactMethod;

public sealed class UpdatePreferredContactMethodCommandValidator : AbstractValidator<UpdatePreferredContactMethodCommand>
{
    public UpdatePreferredContactMethodCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
