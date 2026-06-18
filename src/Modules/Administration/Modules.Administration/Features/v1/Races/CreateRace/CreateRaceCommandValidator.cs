using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Races;

namespace FSH.Modules.Administration.Features.v1.Races.CreateRace;

public sealed class CreateRaceCommandValidator : AbstractValidator<CreateRaceCommand>
{
    public CreateRaceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
