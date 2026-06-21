using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.ProcedureCodes;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ProcedureCodes.CreateProcedureCode;

public sealed class CreateProcedureCodeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateProcedureCodeCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateProcedureCodeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool categoryExists = await dbContext.ProcedureCategories
            .AnyAsync(c => c.Id == command.ProcedureCategoryId, cancellationToken)
            .ConfigureAwait(false);
        if (!categoryExists)
        {
            throw new NotFoundException($"Procedure category {command.ProcedureCategoryId} not found.");
        }

        ProcedureCode entity = ProcedureCode.Create(
            command.Code,
            command.Name,
            command.Description,
            command.ProcedureCategoryId,
            command.CodeSource,
            command.MacroText);
        dbContext.ProcedureCodes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
