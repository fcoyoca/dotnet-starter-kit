using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.IncidentTypes;

namespace FSH.Modules.Administration.Features.v1.IncidentTypes.UpdateIncidentType;

public sealed class UpdateIncidentTypeCommandValidator : AbstractValidator<UpdateIncidentTypeCommand>
{
    public UpdateIncidentTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
