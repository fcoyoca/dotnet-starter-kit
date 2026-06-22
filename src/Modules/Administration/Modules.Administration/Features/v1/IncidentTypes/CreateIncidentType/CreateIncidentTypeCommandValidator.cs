using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.IncidentTypes;

namespace FSH.Modules.Administration.Features.v1.IncidentTypes.CreateIncidentType;

public sealed class CreateIncidentTypeCommandValidator : AbstractValidator<CreateIncidentTypeCommand>
{
    public CreateIncidentTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
