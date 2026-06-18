using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Languages;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Languages.ListLanguages;

public sealed class ListLanguagesQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListLanguagesQuery, IReadOnlyList<LookupItemDto>>
{
    public async ValueTask<IReadOnlyList<LookupItemDto>> Handle(ListLanguagesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        IQueryable<Language> q = dbContext.Languages.AsNoTracking();
        if (query.IsActive.HasValue)
        {
            q = q.Where(l => l.IsActive == query.IsActive.Value);
        }

        List<Language> items = await q.OrderBy(l => l.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        return items.Select(l => new LookupItemDto(l.Id, l.Name, l.IsActive)).ToList();
    }
}
