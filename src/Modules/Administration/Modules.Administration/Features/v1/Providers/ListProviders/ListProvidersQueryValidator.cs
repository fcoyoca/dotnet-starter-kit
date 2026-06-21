using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Providers;

namespace FSH.Modules.Administration.Features.v1.Providers.ListProviders;

public sealed class ListProvidersQueryValidator : AbstractValidator<ListProvidersQuery>
{
    public ListProvidersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
