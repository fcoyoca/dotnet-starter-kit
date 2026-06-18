using FSH.Modules.Administration.Contracts.v1.Ethnicities;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.Ethnicities.CreateEthnicity;

public sealed class CreateEthnicityCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateEthnicityCommand, int>
{
    public async ValueTask<int> Handle(CreateEthnicityCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Ethnicity entity = Ethnicity.Create(command.Name);
        dbContext.Ethnicities.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
