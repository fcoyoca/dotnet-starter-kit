using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.UpdateInsuranceType;

public sealed class UpdateInsuranceTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateInsuranceTypeCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateInsuranceTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.InsuranceType entity = await dbContext.InsuranceTypes
            .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Insurance type {command.Id} not found.");

        if (command.ProcedureCategoryId is { } categoryId)
        {
            bool categoryExists = await dbContext.ProcedureCategories
                .AnyAsync(c => c.Id == categoryId, cancellationToken)
                .ConfigureAwait(false);
            if (!categoryExists)
            {
                throw new NotFoundException($"Procedure category {categoryId} not found.");
            }
        }

        entity.Update(command.Name, command.ProcedureCategoryId, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
