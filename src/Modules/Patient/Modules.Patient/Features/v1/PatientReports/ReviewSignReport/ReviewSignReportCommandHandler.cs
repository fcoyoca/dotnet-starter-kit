using System.Net;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientReports.ReviewSignReport;

public sealed class ReviewSignReportCommandHandler(
    PatientDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator)
    : ICommandHandler<ReviewSignReportCommand, Unit>
{
    public async ValueTask<Unit> Handle(ReviewSignReportCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        PatientReport report = await dbContext.PatientReports
            .FirstOrDefaultAsync(x => x.Id == command.ReportId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Report {command.ReportId} not found.");

        if (report.WorkflowStatus != ReportWorkflowStatus.ReviewRequested)
        {
            throw new CustomException("This report has not been requested for review.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        // Snapshot the reviewer provider's saved signature image (optional). Cross-module read via Contracts.
        string? signaturePath = null;
        if (report.ReviewerProviderId is { } reviewerId)
        {
            try
            {
                var provider = await mediator.Send(new GetProviderByIdQuery(reviewerId), cancellationToken).ConfigureAwait(false);
                signaturePath = provider.SignatureImagePath;
            }
            catch (NotFoundException)
            {
                signaturePath = null; // provider deleted — review-sign with attestation only
            }
        }

        report.ReviewSign(currentUser.GetUserId().ToString(), currentUser.Name, signaturePath);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
