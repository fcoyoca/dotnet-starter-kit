using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.GetInsuranceTypeById;

public sealed class GetInsuranceTypeByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetInsuranceTypeByIdQuery, InsuranceTypeDto>
{
    public async ValueTask<InsuranceTypeDto> Handle(GetInsuranceTypeByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        Domain.InsuranceType entity = await dbContext.InsuranceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Insurance type {query.Id} not found.");
        return new InsuranceTypeDto(entity.Id, entity.Name, entity.IsActive, entity.CreatedAtUtc, entity.UpdatedAtUtc);
    }
}
