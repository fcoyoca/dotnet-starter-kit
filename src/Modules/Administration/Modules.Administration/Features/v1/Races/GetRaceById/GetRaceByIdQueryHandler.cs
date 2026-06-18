using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Races;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Races.GetRaceById;

public sealed class GetRaceByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetRaceByIdQuery, LookupItemDto>
{
    public async ValueTask<LookupItemDto> Handle(GetRaceByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        Domain.Race entity = await dbContext.Races
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Race {query.Id} not found.");
        return new LookupItemDto(entity.Id, entity.Name, entity.IsActive);
    }
}
