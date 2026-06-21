using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Departments;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Departments.ListDepartments;

public sealed class ListDepartmentsQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListDepartmentsQuery, PagedResponse<DepartmentDto>>
{
    public async ValueTask<PagedResponse<DepartmentDto>> Handle(ListDepartmentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        IQueryable<Domain.Department> q = dbContext.Departments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(d => EF.Functions.ILike(d.Name, $"%{term}%"));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(d => d.IsActive == query.IsActive.Value);
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        List<Domain.Department> items = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<DepartmentDto>
        {
            Items = items.Select(d => new DepartmentDto(
                d.Id, d.Name, d.DisplayOrder, d.IsActive,
                d.CreatedAtUtc, d.UpdatedAtUtc)).ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }

    private static IQueryable<Domain.Department> ApplySort(IQueryable<Domain.Department> q, string? sortBy, string? sortDir)
    {
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToUpperInvariant()) switch
        {
            "NAME" => desc ? q.OrderByDescending(d => d.Name) : q.OrderBy(d => d.Name),
            _ => desc
                ? q.OrderByDescending(d => d.DisplayOrder).ThenByDescending(d => d.Name)
                : q.OrderBy(d => d.DisplayOrder).ThenBy(d => d.Name),
        };
    }
}
