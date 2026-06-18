using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Ethnicities;

namespace FSH.Modules.Administration.Features.v1.Ethnicities.CreateEthnicity;

public sealed class CreateEthnicityCommandValidator : AbstractValidator<CreateEthnicityCommand>
{
    public CreateEthnicityCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
