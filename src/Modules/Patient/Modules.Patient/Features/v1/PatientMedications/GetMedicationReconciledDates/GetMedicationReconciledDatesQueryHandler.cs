using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.GetMedicationReconciledDates;

public sealed class GetMedicationReconciledDatesQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<GetMedicationReconciledDatesQuery, IReadOnlyList<MedicationReconciledDateDto>>
{
    public async ValueTask<IReadOnlyList<MedicationReconciledDateDto>> Handle(
        GetMedicationReconciledDatesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await dbContext.MedicationReconciledDates
            .AsNoTracking()
            .Where(x => x.PatientId == query.PatientId)
            .OrderByDescending(x => x.ReconciledOn)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new MedicationReconciledDateDto(
                x.Id, x.PatientId, x.ReconciledOn, x.CreatedByName, x.CreatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
