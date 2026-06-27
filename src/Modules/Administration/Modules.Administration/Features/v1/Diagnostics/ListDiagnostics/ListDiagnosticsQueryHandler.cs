using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Diagnostics;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Diagnostics.ListDiagnostics;

public sealed class ListDiagnosticsQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListDiagnosticsQuery, PagedResponse<DiagnosticDto>>
{
    public async ValueTask<PagedResponse<DiagnosticDto>> Handle(ListDiagnosticsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        IQueryable<Domain.Diagnostic> q = dbContext.Diagnostics.AsNoTracking().Where(d => !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(d =>
                EF.Functions.ILike(d.Code, $"%{term}%") ||
                (d.Description != null && EF.Functions.ILike(d.Description, $"%{term}%")));
        }

        if (query.CodeSourceId.HasValue)
        {
            q = q.Where(d => d.CodeSourceId == query.CodeSourceId.Value);
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(d => d.IsActive == query.IsActive.Value);
        }

        if (query.IsChiropractic.HasValue)
        {
            q = q.Where(d => d.IsChiropractic == query.IsChiropractic.Value);
        }

        if (query.IsBillable.HasValue)
        {
            q = q.Where(d => d.IsBillable == query.IsBillable.Value);
        }

        bool desc = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        q = (query.SortBy?.ToUpperInvariant()) switch
        {
            "DESCRIPTION" => desc ? q.OrderByDescending(d => d.Description) : q.OrderBy(d => d.Description),
            _ => desc ? q.OrderByDescending(d => d.Code) : q.OrderBy(d => d.Code),
        };

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<DiagnosticDto> items = await (
            from d in q.Skip((page - 1) * size).Take(size)
            join s in dbContext.CodeSources on d.CodeSourceId equals s.Id into sourceJoin
            from s in sourceJoin.DefaultIfEmpty()
            select new DiagnosticDto(
                d.Id, d.Code, d.Description, d.LongDescription, d.CodeSourceId,
                s != null ? s.Name : null, d.IsChiropractic, d.IsBillable, d.IsActive,
                d.CreatedAtUtc, d.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<DiagnosticDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
