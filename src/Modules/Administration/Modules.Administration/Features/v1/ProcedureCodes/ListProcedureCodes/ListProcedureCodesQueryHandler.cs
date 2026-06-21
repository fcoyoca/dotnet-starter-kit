using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.ProcedureCodes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ProcedureCodes.ListProcedureCodes;

public sealed class ListProcedureCodesQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListProcedureCodesQuery, PagedResponse<ProcedureCodeDto>>
{
    public async ValueTask<PagedResponse<ProcedureCodeDto>> Handle(ListProcedureCodesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        IQueryable<Domain.ProcedureCode> q = dbContext.ProcedureCodes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(c =>
                EF.Functions.ILike(c.Code, $"%{term}%") ||
                (c.Name != null && EF.Functions.ILike(c.Name, $"%{term}%")) ||
                (c.Description != null && EF.Functions.ILike(c.Description, $"%{term}%")));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(c => c.IsActive == query.IsActive.Value);
        }

        if (query.ProcedureCategoryId.HasValue)
        {
            q = q.Where(c => c.ProcedureCategoryId == query.ProcedureCategoryId.Value);
        }

        bool desc = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        q = (query.SortBy?.ToUpperInvariant()) switch
        {
            "NAME" => desc ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name),
            _ => desc ? q.OrderByDescending(c => c.Code) : q.OrderBy(c => c.Code),
        };

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        List<ProcedureCodeDto> items = await (
            from c in q.Skip((page - 1) * size).Take(size)
            join cat in dbContext.ProcedureCategories on c.ProcedureCategoryId equals cat.Id into catJoin
            from cat in catJoin.DefaultIfEmpty()
            join src in dbContext.CodeSources on c.CodeSourceId equals src.Id into srcJoin
            from src in srcJoin.DefaultIfEmpty()
            select new ProcedureCodeDto(
                c.Id, c.Code, c.Name, c.Description,
                c.ProcedureCategoryId, cat != null ? cat.Name : null,
                c.CodeSourceId, src != null ? src.Name : null,
                c.MacroText, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<ProcedureCodeDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
