using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientProblems;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientProblems.SearchPatientProblems;

public sealed class SearchPatientProblemsQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<SearchPatientProblemsQuery, PagedResponse<PatientProblemDto>>
{
    public async ValueTask<PagedResponse<PatientProblemDto>> Handle(
        SearchPatientProblemsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 100 : query.PageSize;

        IQueryable<Domain.PatientProblem> q = dbContext.PatientProblems
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

        // Active is always shown; Resolved/Inactive are opt-in (mirrors BackChart's filters).
        var statuses = new List<ProblemStatus> { ProblemStatus.Active };
        if (query.IncludeResolved) statuses.Add(ProblemStatus.Resolved);
        if (query.IncludeInactive) statuses.Add(ProblemStatus.Inactive);
        q = q.Where(x => statuses.Contains(x.Status));

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<PatientProblemDto> items = await q
            .OrderByDescending(x => x.IsMedicalAlert)
            .ThenBy(x => x.Status)
            .ThenByDescending(x => x.DiagnosisDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new PatientProblemDto(
                x.Id, x.PatientId, x.IncidentId, x.DiagnosticId, x.DiagnosticCode, x.DiagnosticDescription,
                x.DiagnosisDate, x.Status, x.Notes, x.IsMedicalAlert,
                x.CreatedByName, x.CreatedAtUtc, x.UpdatedByName, x.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PatientProblemDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
