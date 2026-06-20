using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;

namespace FSH.Modules.Administration.Features.v1.PreferredContactMethods.DeletePreferredContactMethod;

public sealed class DeletePreferredContactMethodCommandValidator : AbstractValidator<DeletePreferredContactMethodCommand>
{
    public DeletePreferredContactMethodCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
