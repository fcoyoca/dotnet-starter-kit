using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.ProcedureCategories;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ProcedureCategories.ListProcedureCategories;

public sealed class ListProcedureCategoriesQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListProcedureCategoriesQuery, PagedResponse<ProcedureCategoryDto>>
{
    public async ValueTask<PagedResponse<ProcedureCategoryDto>> Handle(ListProcedureCategoriesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        IQueryable<Domain.ProcedureCategory> q = dbContext.ProcedureCategories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(c => EF.Functions.ILike(c.Name, $"%{term}%"));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(c => c.IsActive == query.IsActive.Value);
        }

        if (query.IsImaging.HasValue)
        {
            q = q.Where(c => c.IsImaging == query.IsImaging.Value);
        }

        bool desc = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        q = desc ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        List<ProcedureCategoryDto> items = await q
            .Skip((page - 1) * size)
            .Take(size)
            .Select(c => new ProcedureCategoryDto(
                c.Id, c.Name, c.Description, c.IsImaging, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<ProcedureCategoryDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
