using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.SearchPatientIncidents;

public sealed class SearchPatientIncidentsQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<SearchPatientIncidentsQuery, PagedResponse<PatientIncidentListItemDto>>
{
    public async ValueTask<PagedResponse<PatientIncidentListItemDto>> Handle(
        SearchPatientIncidentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        var q = dbContext.PatientIncidents
            .Include(x => x.Diagnostics)
            .AsNoTracking()
            .Where(x => x.PatientId == query.PatientId);

        if (!query.IncludeDeleted)
            q = q.Where(x => !x.IsDeleted);

        if (query.IsClosed.HasValue)
            q = q.Where(x => x.IsClosed == query.IsClosed.Value);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        var incidents = await q
            .OrderByDescending(x => x.DateOfLoss)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PatientIncidentListItemDto>
        {
            Items = incidents.Select(x => new PatientIncidentListItemDto(
                x.Id, x.PatientId,
                x.IncidentTypeId, x.DepartmentId,
                x.DateOfInitialVisit, x.DateOfLoss,
                x.IsClosed, x.IsTransfer, x.IsAccident,
                x.AccidentType, x.AccidentState,
                x.PatientStatus,
                x.Diagnostics.Select(d => d.DiagnosticId).ToList(),
                x.CreatedAtUtc, x.UpdatedAtUtc)).ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
