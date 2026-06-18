using FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.PreferredContactMethods.CreatePreferredContactMethod;

public sealed class CreatePreferredContactMethodCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreatePreferredContactMethodCommand, int>
{
    public async ValueTask<int> Handle(CreatePreferredContactMethodCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        PreferredContactMethod entity = PreferredContactMethod.Create(command.Name);
        dbContext.PreferredContactMethods.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
