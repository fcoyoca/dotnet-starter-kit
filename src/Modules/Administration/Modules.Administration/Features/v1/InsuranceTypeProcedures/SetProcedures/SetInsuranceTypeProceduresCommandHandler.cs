using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.SetProcedures;

public sealed class SetInsuranceTypeProceduresCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<SetInsuranceTypeProceduresCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetInsuranceTypeProceduresCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool typeExists = await dbContext.InsuranceTypes
            .AnyAsync(t => t.Id == command.InsuranceTypeId, cancellationToken)
            .ConfigureAwait(false);
        if (!typeExists)
        {
            throw new NotFoundException($"Insurance type {command.InsuranceTypeId} not found.");
        }

        // De-dupe by procedure code (last price wins); clamp negatives to zero.
        Dictionary<Guid, decimal> desired = [];
        foreach (InsuranceTypeProcedureItem item in command.Items)
        {
            desired[item.ProcedureCodeId] = item.Price < 0 ? 0 : item.Price;
        }

        if (desired.Count > 0)
        {
            List<Guid> codeIds = [.. desired.Keys];
            int existingCodes = await dbContext.ProcedureCodes
                .CountAsync(c => codeIds.Contains(c.Id), cancellationToken)
                .ConfigureAwait(false);
            if (existingCodes != codeIds.Count)
            {
                throw new NotFoundException("One or more procedure codes were not found.");
            }
        }

        List<InsuranceTypeProcedure> existing = await dbContext.InsuranceTypeProcedures
            .Where(x => x.InsuranceTypeId == command.InsuranceTypeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Update kept rows in place (preserving CreatedAt), drop deselected ones.
        foreach (InsuranceTypeProcedure row in existing)
        {
            if (desired.TryGetValue(row.ProcedureCodeId, out decimal price))
            {
                if (row.Price != price)
                {
                    row.UpdatePrice(price);
                }

                desired.Remove(row.ProcedureCodeId);
            }
            else
            {
                dbContext.InsuranceTypeProcedures.Remove(row);
            }
        }

        // Add newly selected rows.
        foreach ((Guid codeId, decimal price) in desired)
        {
            dbContext.InsuranceTypeProcedures.Add(
                InsuranceTypeProcedure.Create(command.InsuranceTypeId, codeId, price));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
