using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Races;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Races.ListRaces;

public sealed class ListRacesQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListRacesQuery, IReadOnlyList<LookupItemDto>>
{
    public async ValueTask<IReadOnlyList<LookupItemDto>> Handle(ListRacesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        IQueryable<Domain.Race> q = dbContext.Races.AsNoTracking();
        if (query.IsActive.HasValue)
        {
            q = q.Where(r => r.IsActive == query.IsActive.Value);
        }

        List<Domain.Race> items = await q.OrderBy(r => r.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        return items.Select(r => new LookupItemDto(r.Id, r.Name, r.IsActive)).ToList();
    }
}
