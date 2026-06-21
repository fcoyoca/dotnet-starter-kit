using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.ListInsuranceTypes;

public sealed class ListInsuranceTypesQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListInsuranceTypesQuery, PagedResponse<InsuranceTypeDto>>
{
    public async ValueTask<PagedResponse<InsuranceTypeDto>> Handle(ListInsuranceTypesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        IQueryable<Domain.InsuranceType> q = dbContext.InsuranceTypes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(t => EF.Functions.ILike(t.Name, $"%{term}%"));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(t => t.IsActive == query.IsActive.Value);
        }

        bool desc = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        q = desc ? q.OrderByDescending(t => t.Name) : q.OrderBy(t => t.Name);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        List<InsuranceTypeDto> items = await q
            .Skip((page - 1) * size)
            .Take(size)
            .Select(t => new InsuranceTypeDto(
                t.Id, t.Name, t.IsActive, t.ProcedureCategoryId,
                dbContext.ProcedureCategories.Where(c => c.Id == t.ProcedureCategoryId).Select(c => c.Name).FirstOrDefault(),
                t.CreatedAtUtc, t.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<InsuranceTypeDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
