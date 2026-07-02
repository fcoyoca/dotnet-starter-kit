using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.AllergyReactions;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.AllergyReactions.ListAllergyReactions;

public sealed class ListAllergyReactionsQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListAllergyReactionsQuery, IReadOnlyList<AllergyReactionDto>>
{
    public async ValueTask<IReadOnlyList<AllergyReactionDto>> Handle(ListAllergyReactionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        IQueryable<AllergyReaction> q = dbContext.AllergyReactions.AsNoTracking();
        if (query.IsActive.HasValue)
        {
            q = q.Where(a => a.IsActive == query.IsActive.Value);
        }

        List<AllergyReaction> items = await q.OrderBy(a => a.Term).ToListAsync(cancellationToken).ConfigureAwait(false);
        return items.Select(a => new AllergyReactionDto(a.Id, a.Term, a.SnomedCode, a.IsActive)).ToList();
    }
}
