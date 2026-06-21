using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.CodeSources;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.CodeSources.ListCodeSources;

public sealed class ListCodeSourcesQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListCodeSourcesQuery, IReadOnlyList<LookupItemDto>>
{
    public async ValueTask<IReadOnlyList<LookupItemDto>> Handle(ListCodeSourcesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        IQueryable<Domain.CodeSource> q = dbContext.CodeSources.AsNoTracking();
        if (query.IsActive.HasValue)
        {
            q = q.Where(c => c.IsActive == query.IsActive.Value);
        }

        List<Domain.CodeSource> items = await q.OrderBy(c => c.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        return items.Select(c => new LookupItemDto(c.Id, c.Name, c.IsActive)).ToList();
    }
}
