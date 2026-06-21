using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategories.ListDiagnosticCategories;

public sealed class ListDiagnosticCategoriesQueryValidator : AbstractValidator<ListDiagnosticCategoriesQuery>
{
    public ListDiagnosticCategoriesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
