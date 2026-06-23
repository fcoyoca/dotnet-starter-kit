using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ReportTemplates;

/// <summary>Report fields belonging to the given report type. Active-only by default (the Macros admin);
/// pass <paramref name="IncludeInactive"/> to also return inactive fields (the field manager).</summary>
public sealed record ListReportFieldsQuery(int ReportTypeId, bool IncludeInactive = false)
    : IQuery<IReadOnlyList<ReportFieldDto>>;
