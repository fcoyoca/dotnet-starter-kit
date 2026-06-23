using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ReportTemplates;

public sealed record ListReportTypesQuery(bool? IsActive = null) : IQuery<IReadOnlyList<ReportTypeDto>>;
