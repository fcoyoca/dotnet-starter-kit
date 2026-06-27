using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientReports.SetReportProblems;

public sealed class SetReportProblemsCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<SetReportProblemsCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetReportProblemsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        PatientReport report = await dbContext.PatientReports
            .Include(x => x.AssociatedProblems)
            .FirstOrDefaultAsync(x => x.Id == command.ReportId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Report {command.ReportId} not found.");

        report.SetAssociatedProblems(command.ProblemIds ?? []);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
