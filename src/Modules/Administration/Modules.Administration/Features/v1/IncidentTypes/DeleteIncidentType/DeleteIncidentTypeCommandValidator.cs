using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.IncidentTypes;

namespace FSH.Modules.Administration.Features.v1.IncidentTypes.DeleteIncidentType;

public sealed class DeleteIncidentTypeCommandValidator : AbstractValidator<DeleteIncidentTypeCommand>
{
    public DeleteIncidentTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
