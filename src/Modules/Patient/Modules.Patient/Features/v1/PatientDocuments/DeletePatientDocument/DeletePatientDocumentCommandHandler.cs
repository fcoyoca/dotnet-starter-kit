using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientDocuments;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientDocuments.DeletePatientDocument;

/// <summary>Soft-deletes the document record. The file stays on disk (legacy parity — BackChart
/// retained files and only flagged the row deleted).</summary>
public sealed class DeletePatientDocumentCommandHandler(
    PatientDbContext dbContext,
    ICurrentUser currentUser)
    : ICommandHandler<DeletePatientDocumentCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeletePatientDocumentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.PatientDocument document = await dbContext.PatientDocuments
            .FirstOrDefaultAsync(x => x.Id == command.DocumentId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Document {command.DocumentId} not found.");

        document.Delete(currentUser.GetUserId().ToString());
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
