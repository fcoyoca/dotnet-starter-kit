using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.PreferredContactMethods.GetPreferredContactMethodById;

public sealed class GetPreferredContactMethodByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetPreferredContactMethodByIdQuery, LookupItemDto>
{
    public async ValueTask<LookupItemDto> Handle(GetPreferredContactMethodByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        PreferredContactMethod entity = await dbContext.PreferredContactMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"PreferredContactMethod {query.Id} not found.");
        return new LookupItemDto(entity.Id, entity.Name, entity.IsActive);
    }
}
