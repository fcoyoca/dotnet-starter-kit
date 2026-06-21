using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.RemoveProcedure;

public sealed class RemoveProcedureFromInsuranceTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<RemoveProcedureFromInsuranceTypeCommand, Unit>
{
    public async ValueTask<Unit> Handle(RemoveProcedureFromInsuranceTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.InsuranceTypeProcedure entity = await dbContext.InsuranceTypeProcedures
            .FirstOrDefaultAsync(
                x => x.InsuranceTypeId == command.InsuranceTypeId && x.ProcedureCodeId == command.ProcedureCodeId,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(
                $"Procedure {command.ProcedureCodeId} is not associated with insurance type {command.InsuranceTypeId}.");

        dbContext.InsuranceTypeProcedures.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
