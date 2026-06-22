using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Macros;

namespace FSH.Modules.Administration.Features.v1.Macros.ListMacros;

public sealed class ListMacrosQueryValidator : AbstractValidator<ListMacrosQuery>
{
    public ListMacrosQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
