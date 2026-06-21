using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;

namespace FSH.Modules.Administration.Features.v1.InsuranceCompanies.ListInsuranceCompanies;

public sealed class ListInsuranceCompaniesQueryValidator : AbstractValidator<ListInsuranceCompaniesQuery>
{
    public ListInsuranceCompaniesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
