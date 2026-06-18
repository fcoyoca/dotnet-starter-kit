using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Ethnicities;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Ethnicities.ListEthnicities;

public sealed class ListEthnicitiesQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListEthnicitiesQuery, IReadOnlyList<LookupItemDto>>
{
    public async ValueTask<IReadOnlyList<LookupItemDto>> Handle(ListEthnicitiesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        IQueryable<Ethnicity> q = dbContext.Ethnicities.AsNoTracking();
        if (query.IsActive.HasValue)
        {
            q = q.Where(e => e.IsActive == query.IsActive.Value);
        }

        List<Ethnicity> items = await q.OrderBy(e => e.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        return items.Select(e => new LookupItemDto(e.Id, e.Name, e.IsActive)).ToList();
    }
}
