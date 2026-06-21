using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.ProcedureCodes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ProcedureCodes.UpdateProcedureCode;

public sealed class UpdateProcedureCodeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateProcedureCodeCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateProcedureCodeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.ProcedureCode entity = await dbContext.ProcedureCodes
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Procedure code {command.Id} not found.");

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

        entity.Update(
            command.Code,
            command.Name,
            command.Description,
            command.ProcedureCategoryId,
            command.CodeSource,
            command.MacroText,
            command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
