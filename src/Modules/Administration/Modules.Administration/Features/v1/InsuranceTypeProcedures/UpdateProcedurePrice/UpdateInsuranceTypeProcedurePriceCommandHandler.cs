using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.UpdateProcedurePrice;

public sealed class UpdateInsuranceTypeProcedurePriceCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateInsuranceTypeProcedurePriceCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateInsuranceTypeProcedurePriceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.InsuranceTypeProcedure entity = await dbContext.InsuranceTypeProcedures
            .FirstOrDefaultAsync(
                x => x.InsuranceTypeId == command.InsuranceTypeId && x.ProcedureCodeId == command.ProcedureCodeId,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(
                $"Procedure {command.ProcedureCodeId} is not associated with insurance type {command.InsuranceTypeId}.");

        entity.UpdatePrice(command.Price);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
