using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.ReferralTypes;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ReferralTypes.ListReferralTypes;

public sealed class ListReferralTypesQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListReferralTypesQuery, IReadOnlyList<LookupItemDto>>
{
    public async ValueTask<IReadOnlyList<LookupItemDto>> Handle(ListReferralTypesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        IQueryable<ReferralType> q = dbContext.ReferralTypes.AsNoTracking();
        if (query.IsActive.HasValue)
        {
            q = q.Where(r => r.IsActive == query.IsActive.Value);
        }

        List<ReferralType> items = await q.OrderBy(r => r.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        return items.Select(r => new LookupItemDto(r.Id, r.Name, r.IsActive)).ToList();
    }
}
