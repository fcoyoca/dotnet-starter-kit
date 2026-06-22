using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A clinical report template type (legacy <c>ReportTypes</c> — <c>rtID</c>/<c>rtName</c>), e.g. Initial Evaluation,
/// Progress Report. Cross-tenant system catalog (<see cref="IGlobalEntity"/>); seeded, read-only reference data
/// that drives the Macros admin (report type → fields → macros).
/// </summary>
public sealed class ReportType : AggregateRoot<int>, IGlobalEntity
{
    public string Name { get; init; } = default!;
}
