using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ProcedureCategories;

namespace FSH.Modules.Administration.Features.v1.ProcedureCategories.ListProcedureCategories;

public sealed class ListProcedureCategoriesQueryValidator : AbstractValidator<ListProcedureCategoriesQuery>
{
    public ListProcedureCategoriesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
