using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Ethnicities;

namespace FSH.Modules.Administration.Features.v1.Ethnicities.DeleteEthnicity;

public sealed class DeleteEthnicityCommandValidator : AbstractValidator<DeleteEthnicityCommand>
{
    public DeleteEthnicityCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
