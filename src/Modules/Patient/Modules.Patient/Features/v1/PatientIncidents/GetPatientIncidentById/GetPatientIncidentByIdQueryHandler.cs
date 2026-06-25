using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.GetPatientIncidentById;

public sealed class GetPatientIncidentByIdQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<GetPatientIncidentByIdQuery, PatientIncidentDetailDto>
{
    public async ValueTask<PatientIncidentDetailDto> Handle(
        GetPatientIncidentByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var incident = await dbContext.PatientIncidents
            .Include(x => x.Diagnostics)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.IncidentId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Incident {query.IncidentId} not found.");

        return new PatientIncidentDetailDto(
            incident.Id, incident.PatientId,
            incident.IncidentTypeId, incident.DepartmentId,
            incident.DateOfInitialVisit, incident.DateOfLoss,
            incident.IsClosed, incident.IsTransfer, incident.IsAccident,
            incident.AccidentType, incident.AccidentState,
            incident.PatientStatus,
            incident.Diagnostics.Select(d => d.DiagnosticId).ToList(),
            incident.CreatedAtUtc, incident.UpdatedAtUtc,
            incident.Comments, incident.SummaryOfCare, incident.AdherenceToPlan);
    }
}
