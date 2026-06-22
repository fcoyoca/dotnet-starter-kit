using FSH.Modules.Administration.Contracts.v1.IncidentTypes;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.IncidentTypes.CreateIncidentType;

public sealed class CreateIncidentTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateIncidentTypeCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateIncidentTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        IncidentType entity = IncidentType.Create(command.Name);
        dbContext.IncidentTypes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
