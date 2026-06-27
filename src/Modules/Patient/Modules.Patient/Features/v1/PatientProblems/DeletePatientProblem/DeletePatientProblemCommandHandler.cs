using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientProblems;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientProblems.DeletePatientProblem;

public sealed class DeletePatientProblemCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<DeletePatientProblemCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeletePatientProblemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        PatientProblem problem = await dbContext.PatientProblems
            .FirstOrDefaultAsync(x => x.Id == command.ProblemId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Problem {command.ProblemId} not found.");

        problem.Delete(currentUser.Name ?? currentUser.GetUserId().ToString());
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
