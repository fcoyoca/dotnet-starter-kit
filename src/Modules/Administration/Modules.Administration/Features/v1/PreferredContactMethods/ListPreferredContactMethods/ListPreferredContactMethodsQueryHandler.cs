using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.PreferredContactMethods.ListPreferredContactMethods;

public sealed class ListPreferredContactMethodsQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListPreferredContactMethodsQuery, IReadOnlyList<LookupItemDto>>
{
    public async ValueTask<IReadOnlyList<LookupItemDto>> Handle(ListPreferredContactMethodsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        IQueryable<PreferredContactMethod> q = dbContext.PreferredContactMethods.AsNoTracking();
        if (query.IsActive.HasValue)
        {
            q = q.Where(p => p.IsActive == query.IsActive.Value);
        }

        List<PreferredContactMethod> items = await q.OrderBy(p => p.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        return items.Select(p => new LookupItemDto(p.Id, p.Name, p.IsActive)).ToList();
    }
}
