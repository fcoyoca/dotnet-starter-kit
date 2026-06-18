using FSH.Modules.Administration.Contracts.v1.ReferralTypes;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.ReferralTypes.CreateReferralType;

public sealed class CreateReferralTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateReferralTypeCommand, int>
{
    public async ValueTask<int> Handle(CreateReferralTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ReferralType entity = ReferralType.Create(command.Name);
        dbContext.ReferralTypes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
