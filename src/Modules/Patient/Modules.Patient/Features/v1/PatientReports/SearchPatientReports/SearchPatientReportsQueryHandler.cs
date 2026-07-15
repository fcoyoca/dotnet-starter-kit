using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientReports.SearchPatientReports;

public sealed class SearchPatientReportsQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<SearchPatientReportsQuery, PagedResponse<PatientReportListItemDto>>
{
    public async ValueTask<PagedResponse<PatientReportListItemDto>> Handle(
        SearchPatientReportsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        IQueryable<Domain.PatientReport> q = dbContext.PatientReports.AsNoTracking();

        if (query.IncidentId.HasValue)
        {
            q = q.Where(x => x.IncidentId == query.IncidentId.Value);
        }

        if (query.PatientId.HasValue)
        {
            q = q.Where(x => x.PatientId == query.PatientId.Value);
        }

        // PatientReport is ISoftDeletable, so a global query filter hides deleted rows.
        // Bypass only that named filter to surface them; tenant scoping stays in force.
        if (query.IncludeDeleted)
        {
            q = q.IgnoreQueryFilters([QueryFilters.SoftDelete]);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(x =>
                x.FieldValues.Any(f => EF.Functions.ILike(f.Text, $"%{term}%")) ||
                x.Addendums.Any(a => EF.Functions.ILike(a.Text, $"%{term}%")));
        }

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<PatientReportListItemDto> items = await q
            .OrderByDescending(x => x.ReportDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new PatientReportListItemDto(
                x.Id, x.IncidentId, x.PatientId, x.ReportTypeId, x.ReportDate, x.Version,
                x.ProviderId, x.ClinicId, x.IsNoShow, x.WorkflowStatus, x.IsSigned,
                x.SignedByName, x.SignedOnUtc, x.CreatedAtUtc, x.UpdatedAtUtc, x.IsDeleted))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PatientReportListItemDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
