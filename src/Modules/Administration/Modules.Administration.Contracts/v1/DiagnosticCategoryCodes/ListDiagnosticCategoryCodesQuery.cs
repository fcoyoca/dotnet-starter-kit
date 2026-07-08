using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.DiagnosticCategoryCodes;

/// <summary>
/// Lists every diagnostic (ICD) code associated with one diagnostic category. Non-paginated — the
/// association set per category is small and the editor renders them all against the full ICD list.
/// </summary>
public sealed record ListDiagnosticCategoryCodesQuery(Guid CategoryId)
    : IQuery<IReadOnlyList<DiagnosticCategoryCodeDto>>;
