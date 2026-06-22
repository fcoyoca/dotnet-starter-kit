using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.PatientDocumentTypes.UpdatePatientDocumentType;

public sealed class UpdatePatientDocumentTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdatePatientDocumentTypeCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdatePatientDocumentTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.PatientDocumentType entity = await dbContext.PatientDocumentTypes
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Patient document type {command.Id} not found.");

        entity.Update(command.Name, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
