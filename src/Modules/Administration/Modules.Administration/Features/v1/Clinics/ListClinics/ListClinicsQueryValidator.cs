using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Clinics;

namespace FSH.Modules.Administration.Features.v1.Clinics.ListClinics;

public sealed class ListClinicsQueryValidator : AbstractValidator<ListClinicsQuery>
{
    public ListClinicsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
