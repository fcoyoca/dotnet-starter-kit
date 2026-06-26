using System.Net;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientReports.SignPatientReport;

public sealed class SignPatientReportCommandHandler(
    PatientDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator)
    : ICommandHandler<SignPatientReportCommand, Unit>
{
    public async ValueTask<Unit> Handle(SignPatientReportCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        PatientReport report = await dbContext.PatientReports
            .FirstOrDefaultAsync(x => x.Id == command.ReportId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Report {command.ReportId} not found.");

        if (report.IsSigned)
        {
            throw new CustomException("This report is already signed.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        // Snapshot the provider's saved signature image (optional). Cross-module read via Contracts.
        string? signatureImagePath = null;
        if (report.ProviderId is { } providerId)
        {
            try
            {
                var provider = await mediator.Send(new GetProviderByIdQuery(providerId), cancellationToken).ConfigureAwait(false);
                signatureImagePath = provider.SignatureImagePath;
            }
            catch (NotFoundException)
            {
                signatureImagePath = null; // provider deleted — sign with attestation only
            }
        }

        string userId = currentUser.GetUserId().ToString();
        report.Sign(userId, currentUser.Name, signatureImagePath);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
