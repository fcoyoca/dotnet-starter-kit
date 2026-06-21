using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.CreateInsuranceType;

public sealed class CreateInsuranceTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateInsuranceTypeCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateInsuranceTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

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

        InsuranceType entity = InsuranceType.Create(command.Name, command.ProcedureCategoryId);
        dbContext.InsuranceTypes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
