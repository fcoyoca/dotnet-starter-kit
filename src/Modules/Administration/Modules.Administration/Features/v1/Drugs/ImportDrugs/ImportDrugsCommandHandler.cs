using FSH.Modules.Administration.Contracts.v1.Drugs;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Drugs.ImportDrugs;

public sealed class ImportDrugsCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<ImportDrugsCommand, int>
{
    public async ValueTask<int> Handle(ImportDrugsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var rxCuis = command.Items.Select(i => i.RxCui).ToList();
        Dictionary<string, Drug> existing = await dbContext.Drugs
            .Where(d => d.RxCui != null && rxCuis.Contains(d.RxCui))
            .ToDictionaryAsync(d => d.RxCui!, cancellationToken)
            .ConfigureAwait(false);

        int affected = 0;
        foreach (var item in command.Items)
        {
            if (existing.TryGetValue(item.RxCui, out Drug? drug))
            {
                drug.Update(item.Name, drug.RxAui, item.RxCui, item.Tty, "RXNORM", item.RxCui, isActive: true);
            }
            else
            {
                dbContext.Drugs.Add(Drug.Create(item.Name, rxAui: null, rxCui: item.RxCui, tty: item.Tty,
                    sab: "RXNORM", code: item.RxCui));
            }
            affected++;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return affected;
    }
}
