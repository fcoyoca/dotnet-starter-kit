using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.CreatePatientIncident;

public sealed class CreatePatientIncidentCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<CreatePatientIncidentCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePatientIncidentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool patientExists = await dbContext.Patients
            .AnyAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false);
        if (!patientExists)
            throw new NotFoundException($"Patient {command.PatientId} not found.");

        var incident = PatientIncident.Create(
            command.PatientId,
            command.IncidentTypeId,
            command.DepartmentId,
            command.DateOfInitialVisit,
            command.DateOfLoss,
            command.IsTransfer,
            command.IsAccident,
            command.AccidentType,
            command.AccidentState,
            command.Comments);

        if (command.DiagnosticIds is { Count: > 0 })
            incident.SetDiagnostics(command.DiagnosticIds.ToList());

        dbContext.PatientIncidents.Add(incident);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return incident.Id;
    }
}
