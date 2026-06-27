using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientProblems;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientProblems.UpdatePatientProblem;

public sealed class UpdatePatientProblemCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<UpdatePatientProblemCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdatePatientProblemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        PatientProblem problem = await dbContext.PatientProblems
            .FirstOrDefaultAsync(x => x.Id == command.ProblemId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Problem {command.ProblemId} not found.");

        problem.Update(
            command.DiagnosticId,
            command.DiagnosticCode,
            command.DiagnosticDescription,
            command.DiagnosisDate,
            command.Status,
            command.Notes,
            command.IsMedicalAlert,
            currentUser.GetUserId().ToString(),
            currentUser.Name);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
