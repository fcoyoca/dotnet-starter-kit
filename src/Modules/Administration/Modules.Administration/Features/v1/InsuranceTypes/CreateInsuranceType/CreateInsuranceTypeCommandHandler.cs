using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.CreateInsuranceType;

public sealed class CreateInsuranceTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateInsuranceTypeCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateInsuranceTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        InsuranceType entity = InsuranceType.Create(command.Name);
        dbContext.InsuranceTypes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
