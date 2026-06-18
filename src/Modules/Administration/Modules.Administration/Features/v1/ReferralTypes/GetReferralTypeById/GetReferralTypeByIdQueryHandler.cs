using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.ReferralTypes;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ReferralTypes.GetReferralTypeById;

public sealed class GetReferralTypeByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetReferralTypeByIdQuery, LookupItemDto>
{
    public async ValueTask<LookupItemDto> Handle(GetReferralTypeByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ReferralType entity = await dbContext.ReferralTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"ReferralType {query.Id} not found.");
        return new LookupItemDto(entity.Id, entity.Name, entity.IsActive);
    }
}
