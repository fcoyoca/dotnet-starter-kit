using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.ListInsuranceTypes;

public sealed class ListInsuranceTypesQueryValidator : AbstractValidator<ListInsuranceTypesQuery>
{
    public ListInsuranceTypesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
