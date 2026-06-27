using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Diagnostics;

public sealed record ListDiagnosticsQuery(
    string? Search = null,
    int? CodeSourceId = null,
    bool? IsActive = null,
    bool? IsChiropractic = null,
    bool? IsBillable = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<DiagnosticDto>>;
