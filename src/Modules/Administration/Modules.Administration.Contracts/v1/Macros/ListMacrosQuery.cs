using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Macros;

public sealed record ListMacrosQuery(
    string? Search = null,
    bool? IsActive = null,
    int? ReportFieldId = null,
    bool? General = null,
    Guid? UseableByUserId = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<MacroDto>>;
