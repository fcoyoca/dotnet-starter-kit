using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.ListCustomDiagnostics;

public sealed class ListCustomDiagnosticsQueryValidator : AbstractValidator<ListCustomDiagnosticsQuery>
{
    public ListCustomDiagnosticsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
