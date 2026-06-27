using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Diagnostics;

namespace FSH.Modules.Administration.Features.v1.Diagnostics.ListDiagnostics;

public sealed class ListDiagnosticsQueryValidator : AbstractValidator<ListDiagnosticsQuery>
{
    public ListDiagnosticsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
