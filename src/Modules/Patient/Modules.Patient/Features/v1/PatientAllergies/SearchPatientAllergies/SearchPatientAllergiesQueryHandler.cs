using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.SearchPatientAllergies;

public sealed class SearchPatientAllergiesQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<SearchPatientAllergiesQuery, PagedResponse<PatientAllergyDto>>
{
    public async ValueTask<PagedResponse<PatientAllergyDto>> Handle(
        SearchPatientAllergiesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 100 : query.PageSize;

        IQueryable<Domain.PatientAllergy> q = dbContext.PatientAllergies
            .AsNoTracking()
            .Where(x => x.PatientId == query.PatientId);

        if (!query.IncludeInactive)
        {
            q = q.Where(x => x.IsActive);
        }

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<PatientAllergyDto> items = await q
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.DateNoted)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new PatientAllergyDto(
                x.Id, x.PatientId, x.DrugName, x.RxAui, x.Reaction, x.Comments,
                x.DateNoted, x.IsActive, x.CreatedByName, x.CreatedAtUtc, x.UpdatedByName, x.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PatientAllergyDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
