using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;

namespace FSH.Modules.Administration.Features.v1.PreferredContactMethods.CreatePreferredContactMethod;

public sealed class CreatePreferredContactMethodCommandValidator : AbstractValidator<CreatePreferredContactMethodCommand>
{
    public CreatePreferredContactMethodCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
