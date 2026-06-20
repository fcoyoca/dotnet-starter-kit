using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Clinics;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Clinics.GetClinicById;

public sealed class GetClinicByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetClinicByIdQuery, ClinicDto>
{
    public async ValueTask<ClinicDto> Handle(GetClinicByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        Domain.Clinic entity = await dbContext.Clinics
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Clinic {query.Id} not found.");
        return new ClinicDto(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.Address1,
            entity.Address2,
            entity.City,
            entity.State,
            entity.Zip,
            entity.Phone,
            entity.IsActive,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
    }
}
