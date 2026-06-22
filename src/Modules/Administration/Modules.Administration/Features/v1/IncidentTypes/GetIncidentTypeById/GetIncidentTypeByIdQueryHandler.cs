using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.IncidentTypes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.IncidentTypes.GetIncidentTypeById;

public sealed class GetIncidentTypeByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetIncidentTypeByIdQuery, IncidentTypeDto>
{
    public async ValueTask<IncidentTypeDto> Handle(GetIncidentTypeByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        IncidentTypeDto? dto = await dbContext.IncidentTypes
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new IncidentTypeDto(c.Id, c.Name, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return dto ?? throw new NotFoundException($"Incident type {query.Id} not found.");
    }
}
