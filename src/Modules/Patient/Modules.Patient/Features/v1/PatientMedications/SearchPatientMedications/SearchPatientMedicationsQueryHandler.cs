using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.SearchPatientMedications;

public sealed class SearchPatientMedicationsQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<SearchPatientMedicationsQuery, PagedResponse<PatientMedicationDto>>
{
    public async ValueTask<PagedResponse<PatientMedicationDto>> Handle(
        SearchPatientMedicationsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 100 : query.PageSize;

        IQueryable<Domain.PatientMedication> q = dbContext.PatientMedications
            .AsNoTracking()
            .Where(x => x.PatientId == query.PatientId);

        if (!query.IncludeInactive)
        {
            q = q.Where(x => x.IsActive);
        }

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<PatientMedicationDto> items = await q
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.StartDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(m => new PatientMedicationDto(
                m.Id, m.PatientId, m.DrugName, m.RxAui, m.RxCode, m.Ndc, m.Prescriber,
                m.StartDate, m.EndDate, m.DoseValue, m.DoseUnitId, m.DosePeriodValue, m.DosePeriodUnit,
                m.Instructions, m.Indication, m.IsActive,
                m.CreatedByName, m.CreatedAtUtc, m.UpdatedByName, m.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PatientMedicationDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
