using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategories.ListDiagnosticCategories;

public sealed class ListDiagnosticCategoriesQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListDiagnosticCategoriesQuery, PagedResponse<DiagnosticCategoryDto>>
{
    public async ValueTask<PagedResponse<DiagnosticCategoryDto>> Handle(ListDiagnosticCategoriesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        IQueryable<Domain.DiagnosticCategory> q = dbContext.DiagnosticCategories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(c => EF.Functions.ILike(c.Name, $"%{term}%"));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(c => c.IsActive == query.IsActive.Value);
        }

        bool desc = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        q = desc ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        List<DiagnosticCategoryDto> items = await q
            .Skip((page - 1) * size)
            .Take(size)
            .Select(c => new DiagnosticCategoryDto(c.Id, c.Name, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<DiagnosticCategoryDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
