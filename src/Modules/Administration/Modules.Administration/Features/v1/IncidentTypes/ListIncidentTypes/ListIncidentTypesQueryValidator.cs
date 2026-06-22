using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.IncidentTypes;

namespace FSH.Modules.Administration.Features.v1.IncidentTypes.ListIncidentTypes;

public sealed class ListIncidentTypesQueryValidator : AbstractValidator<ListIncidentTypesQuery>
{
    public ListIncidentTypesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
