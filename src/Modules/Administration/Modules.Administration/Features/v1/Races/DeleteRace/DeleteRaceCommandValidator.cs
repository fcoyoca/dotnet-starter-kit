using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Races;

namespace FSH.Modules.Administration.Features.v1.Races.DeleteRace;

public sealed class DeleteRaceCommandValidator : AbstractValidator<DeleteRaceCommand>
{
    public DeleteRaceCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
