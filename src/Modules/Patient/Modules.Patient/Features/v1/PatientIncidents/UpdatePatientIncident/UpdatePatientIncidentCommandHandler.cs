using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.UpdatePatientIncident;

public sealed class UpdatePatientIncidentCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<UpdatePatientIncidentCommand>
{
    public async ValueTask<Unit> Handle(UpdatePatientIncidentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var incident = await dbContext.PatientIncidents
            .Include(x => x.Diagnostics)
            .FirstOrDefaultAsync(x => x.Id == command.IncidentId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Incident {command.IncidentId} not found.");

        incident.Update(
            command.IncidentTypeId,
            command.DepartmentId,
            command.DateOfInitialVisit,
            command.DateOfLoss,
            command.IsTransfer,
            command.IsAccident,
            command.AccidentType,
            command.AccidentState,
            command.Comments,
            command.SummaryOfCare,
            command.AdherenceToPlan,
            command.PatientStatus,
            command.IsClosed,
            command.DiagnosticIds?.ToList() ?? []);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
