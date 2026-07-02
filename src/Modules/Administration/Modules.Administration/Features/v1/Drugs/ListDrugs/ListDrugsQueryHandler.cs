using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Drugs.ListDrugs;

public sealed class ListDrugsQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListDrugsQuery, PagedResponse<DrugDto>>
{
    public async ValueTask<PagedResponse<DrugDto>> Handle(ListDrugsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        IQueryable<Domain.Drug> q = dbContext.Drugs.AsNoTracking().Where(d => !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(d =>
                EF.Functions.ILike(d.Name, $"%{term}%") ||
                (d.RxCui != null && EF.Functions.ILike(d.RxCui, $"{term}%")));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(d => d.IsActive == query.IsActive.Value);
        }

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<DrugDto> items = await q
            .OrderBy(d => d.Name)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(d => new DrugDto(
                d.Id, d.Name, d.RxAui, d.RxCui, d.Tty, d.Sab, d.Code,
                d.IsActive, d.CreatedAtUtc, d.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<DrugDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
