using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Races;

namespace FSH.Modules.Administration.Features.v1.Races.UpdateRace;

public sealed class UpdateRaceCommandValidator : AbstractValidator<UpdateRaceCommand>
{
    public UpdateRaceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
