using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Drugs;

namespace FSH.Modules.Administration.Features.v1.Drugs.SearchRxNav;

public sealed class SearchRxNavQueryValidator : AbstractValidator<SearchRxNavQuery>
{
    public SearchRxNavQueryValidator()
    {
        RuleFor(x => x.Term).NotEmpty().MinimumLength(3).MaximumLength(256);
    }
}
