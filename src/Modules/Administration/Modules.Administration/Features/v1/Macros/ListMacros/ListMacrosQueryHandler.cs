using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Macros;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Macros.ListMacros;

public sealed class ListMacrosQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListMacrosQuery, PagedResponse<MacroDto>>
{
    public async ValueTask<PagedResponse<MacroDto>> Handle(ListMacrosQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        IQueryable<Domain.Macro> q = dbContext.Macros.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(c => EF.Functions.ILike(c.Name, $"%{term}%"));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(c => c.IsActive == query.IsActive.Value);
        }

        // Scope to a report field, or to the "All (General)" (unassigned) bucket.
        if (query.General == true)
        {
            q = q.Where(c => c.ReportFieldId == null);
        }
        else if (query.ReportFieldId.HasValue)
        {
            q = q.Where(c => c.ReportFieldId == query.ReportFieldId.Value);
        }

        if (query.UseableByUserId.HasValue)
        {
            q = q.Where(c => c.UseableByUserId == query.UseableByUserId.Value);
        }

        bool desc = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        q = desc ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        List<MacroDto> items = await (
            from c in q.Skip((page - 1) * size).Take(size)
            join f in dbContext.ReportFields on c.ReportFieldId equals f.Id into fieldJoin
            from f in fieldJoin.DefaultIfEmpty()
            select new MacroDto(
                c.Id, c.Name, c.Text,
                c.ReportFieldId, f != null ? f.Name : null, f != null ? f.Category : null,
                c.UseableByUserId, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<MacroDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
