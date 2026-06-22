using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.PatientDocumentTypes.DeletePatientDocumentType;

public sealed class DeletePatientDocumentTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeletePatientDocumentTypeCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeletePatientDocumentTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.PatientDocumentType entity = await dbContext.PatientDocumentTypes
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Patient document type {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
