using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.GetPatientMedicationById;

public sealed class GetPatientMedicationByIdQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<GetPatientMedicationByIdQuery, PatientMedicationDto>
{
    public async ValueTask<PatientMedicationDto> Handle(GetPatientMedicationByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        PatientMedicationDto? dto = await dbContext.PatientMedications
            .AsNoTracking()
            .Where(m => m.Id == query.MedicationId)
            .Select(m => new PatientMedicationDto(
                m.Id, m.PatientId, m.DrugName, m.RxAui, m.RxCode, m.Ndc, m.Prescriber,
                m.StartDate, m.EndDate, m.DoseValue, m.DoseUnitId, m.DosePeriodValue, m.DosePeriodUnit,
                m.Instructions, m.Indication, m.IsActive,
                m.CreatedByName, m.CreatedAtUtc, m.UpdatedByName, m.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return dto ?? throw new NotFoundException($"Medication {query.MedicationId} not found.");
    }
}
