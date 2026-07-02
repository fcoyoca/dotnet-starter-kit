using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.AllergyReactions;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.AllergyReactions.GetAllergyReactionById;

public sealed class GetAllergyReactionByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetAllergyReactionByIdQuery, AllergyReactionDto>
{
    public async ValueTask<AllergyReactionDto> Handle(GetAllergyReactionByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        AllergyReaction entity = await dbContext.AllergyReactions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"AllergyReaction {query.Id} not found.");
        return new AllergyReactionDto(entity.Id, entity.Term, entity.SnomedCode, entity.IsActive);
    }
}
