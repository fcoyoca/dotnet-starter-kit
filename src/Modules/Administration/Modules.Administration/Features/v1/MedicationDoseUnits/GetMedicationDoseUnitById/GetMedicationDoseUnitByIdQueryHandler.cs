using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.MedicationDoseUnits.GetMedicationDoseUnitById;

public sealed class GetMedicationDoseUnitByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetMedicationDoseUnitByIdQuery, MedicationDoseUnitDto>
{
    public async ValueTask<MedicationDoseUnitDto> Handle(GetMedicationDoseUnitByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        MedicationDoseUnit entity = await dbContext.MedicationDoseUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"MedicationDoseUnit {query.Id} not found.");
        return new MedicationDoseUnitDto(entity.Id, entity.Name, entity.IsActive);
    }
}
