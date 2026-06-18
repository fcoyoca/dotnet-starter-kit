using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.SmokingStatuses;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.SmokingStatuses.GetSmokingStatusById;

public sealed class GetSmokingStatusByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetSmokingStatusByIdQuery, LookupItemDto>
{
    public async ValueTask<LookupItemDto> Handle(GetSmokingStatusByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        SmokingStatus entity = await dbContext.SmokingStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"SmokingStatus {query.Id} not found.");
        return new LookupItemDto(entity.Id, entity.Name, entity.IsActive, entity.SnomedCode);
    }
}
