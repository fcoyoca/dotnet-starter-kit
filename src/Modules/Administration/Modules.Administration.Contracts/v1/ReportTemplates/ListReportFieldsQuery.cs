using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ReportTemplates;

/// <summary>Active report fields belonging to the given report type (for the Macros admin).</summary>
public sealed record ListReportFieldsQuery(int ReportTypeId) : IQuery<IReadOnlyList<ReportFieldDto>>;
