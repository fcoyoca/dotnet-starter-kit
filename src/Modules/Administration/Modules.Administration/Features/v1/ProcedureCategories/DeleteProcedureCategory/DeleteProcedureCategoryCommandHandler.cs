using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.ProcedureCategories;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ProcedureCategories.DeleteProcedureCategory;

public sealed class DeleteProcedureCategoryCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteProcedureCategoryCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteProcedureCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.ProcedureCategory entity = await dbContext.ProcedureCategories
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Procedure category {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
