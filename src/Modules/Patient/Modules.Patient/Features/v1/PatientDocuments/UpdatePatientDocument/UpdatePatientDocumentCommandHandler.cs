using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientDocuments;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientDocuments.UpdatePatientDocument;

public sealed class UpdatePatientDocumentCommandHandler(
    PatientDbContext dbContext,
    ICurrentUser currentUser)
    : ICommandHandler<UpdatePatientDocumentCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdatePatientDocumentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.PatientDocument document = await dbContext.PatientDocuments
            .FirstOrDefaultAsync(x => x.Id == command.DocumentId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Document {command.DocumentId} not found.");

        document.UpdateDetails(
            command.DocumentTypeId,
            command.Notes,
            currentUser.GetUserId().ToString(),
            currentUser.Name);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
