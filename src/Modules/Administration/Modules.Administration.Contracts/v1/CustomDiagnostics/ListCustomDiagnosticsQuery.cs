using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;

public sealed record ListCustomDiagnosticsQuery(
    string? Search = null,
    bool? IsActive = null,
    bool? IsChiropractic = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<CustomDiagnosticDto>>;
