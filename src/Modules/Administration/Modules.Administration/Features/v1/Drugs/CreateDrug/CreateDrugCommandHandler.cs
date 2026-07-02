using FSH.Modules.Administration.Contracts.v1.Drugs;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.Drugs.CreateDrug;

public sealed class CreateDrugCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateDrugCommand, int>
{
    public async ValueTask<int> Handle(CreateDrugCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Drug drug = Drug.Create(command.Name, command.RxAui, command.RxCui, command.Tty, command.Sab, command.Code);
        dbContext.Drugs.Add(drug);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return drug.Id;
    }
}
