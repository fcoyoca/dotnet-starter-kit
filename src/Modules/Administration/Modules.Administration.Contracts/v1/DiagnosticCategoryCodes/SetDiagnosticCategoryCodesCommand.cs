using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.DiagnosticCategoryCodes;

/// <summary>
/// Replaces the full set of diagnostic (ICD) code associations for one diagnostic category
/// (legacy <c>ascDiagnosticCategories</c> "insertString" save). Codes present become/stay associated;
/// codes absent are removed. An empty list clears the category's associations.
/// </summary>
public sealed record SetDiagnosticCategoryCodesCommand(
    Guid CategoryId,
    IReadOnlyList<int> DiagnosticIds) : ICommand<Unit>;
