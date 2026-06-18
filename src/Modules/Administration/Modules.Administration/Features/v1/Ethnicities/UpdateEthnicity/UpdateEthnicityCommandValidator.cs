using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Ethnicities;

namespace FSH.Modules.Administration.Features.v1.Ethnicities.UpdateEthnicity;

public sealed class UpdateEthnicityCommandValidator : AbstractValidator<UpdateEthnicityCommand>
{
    public UpdateEthnicityCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
