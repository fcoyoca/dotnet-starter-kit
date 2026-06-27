using FSH.Framework.Core.Context;
using FSH.Modules.Patient.Contracts.v1.PatientProblems;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;

namespace FSH.Modules.Patient.Features.v1.PatientProblems.CreatePatientProblem;

public sealed class CreatePatientProblemCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<CreatePatientProblemCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePatientProblemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        PatientProblem problem = PatientProblem.Create(
            command.PatientId,
            command.DiagnosticId,
            command.DiagnosticCode,
            command.DiagnosticDescription,
            command.DiagnosisDate,
            command.Status,
            command.Notes,
            command.IsMedicalAlert,
            command.IncidentId,
            currentUser.GetUserId().ToString(),
            currentUser.Name);

        dbContext.PatientProblems.Add(problem);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return problem.Id;
    }
}
