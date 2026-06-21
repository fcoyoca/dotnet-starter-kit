using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.ListCustomDiagnostics;

public sealed class ListCustomDiagnosticsQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListCustomDiagnosticsQuery, PagedResponse<CustomDiagnosticDto>>
{
    public async ValueTask<PagedResponse<CustomDiagnosticDto>> Handle(ListCustomDiagnosticsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        IQueryable<Domain.CustomDiagnostic> q = dbContext.CustomDiagnostics.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(c =>
                EF.Functions.ILike(c.Code, $"%{term}%") ||
                (c.Description != null && EF.Functions.ILike(c.Description, $"%{term}%")));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(c => c.IsActive == query.IsActive.Value);
        }

        if (query.IsChiropractic.HasValue)
        {
            q = q.Where(c => c.IsChiropractic == query.IsChiropractic.Value);
        }

        bool desc = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        q = (query.SortBy?.ToUpperInvariant()) switch
        {
            "DESCRIPTION" => desc ? q.OrderByDescending(c => c.Description) : q.OrderBy(c => c.Description),
            _ => desc ? q.OrderByDescending(c => c.Code) : q.OrderBy(c => c.Code),
        };

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        List<CustomDiagnosticDto> items = await q
            .Skip((page - 1) * size)
            .Take(size)
            .Select(c => new CustomDiagnosticDto(
                c.Id, c.Code, c.Description, c.LongDescription, c.IsChiropractic,
                c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<CustomDiagnosticDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
