using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Ethnicities;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Ethnicities.GetEthnicityById;

public sealed class GetEthnicityByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetEthnicityByIdQuery, LookupItemDto>
{
    public async ValueTask<LookupItemDto> Handle(GetEthnicityByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        Ethnicity entity = await dbContext.Ethnicities
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Ethnicity {query.Id} not found.");
        return new LookupItemDto(entity.Id, entity.Name, entity.IsActive);
    }
}
