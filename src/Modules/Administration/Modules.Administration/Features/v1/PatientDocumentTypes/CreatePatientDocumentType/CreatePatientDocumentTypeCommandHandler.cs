using FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.PatientDocumentTypes.CreatePatientDocumentType;

public sealed class CreatePatientDocumentTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreatePatientDocumentTypeCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePatientDocumentTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        PatientDocumentType entity = PatientDocumentType.Create(command.Name);
        dbContext.PatientDocumentTypes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
