using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.SearchPatientNotes;

public sealed class SearchPatientNotesQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<SearchPatientNotesQuery, PagedResponse<PatientNoteDto>>
{
    public async ValueTask<PagedResponse<PatientNoteDto>> Handle(
        SearchPatientNotesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 100 : query.PageSize;

        IQueryable<Domain.PatientNote> q = dbContext.PatientNotes
            .AsNoTracking()
            .Where(x => x.PatientId == query.PatientId);

        if (!query.IncludeDeleted)
        {
            q = q.Where(x => !x.IsDeleted);
        }

        if (query.MedicalAlertsOnly)
        {
            q = q.Where(x => x.IsMedicalAlert);
        }

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<PatientNoteDto> items = await q
            .OrderByDescending(x => x.IsMedicalAlert)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(n => new PatientNoteDto(
                n.Id, n.PatientId, n.Name, n.Description, n.IsMedicalAlert,
                n.CreatedByName, n.CreatedAtUtc, n.UpdatedByName, n.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PatientNoteDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
