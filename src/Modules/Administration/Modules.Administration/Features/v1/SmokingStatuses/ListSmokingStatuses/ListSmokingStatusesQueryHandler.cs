using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.SmokingStatuses;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.SmokingStatuses.ListSmokingStatuses;

public sealed class ListSmokingStatusesQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListSmokingStatusesQuery, IReadOnlyList<LookupItemDto>>
{
    public async ValueTask<IReadOnlyList<LookupItemDto>> Handle(ListSmokingStatusesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        IQueryable<SmokingStatus> q = dbContext.SmokingStatuses.AsNoTracking();
        if (query.IsActive.HasValue)
        {
            q = q.Where(s => s.IsActive == query.IsActive.Value);
        }

        List<SmokingStatus> items = await q.OrderBy(s => s.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        return items.Select(s => new LookupItemDto(s.Id, s.Name, s.IsActive, s.SnomedCode)).ToList();
    }
}
