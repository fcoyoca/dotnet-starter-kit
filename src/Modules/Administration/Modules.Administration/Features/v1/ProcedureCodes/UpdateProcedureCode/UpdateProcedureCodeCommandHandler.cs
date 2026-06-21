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

        bool categoryExists = await dbContext.ProcedureCategories
            .AnyAsync(c => c.Id == command.ProcedureCategoryId, cancellationToken)
            .ConfigureAwait(false);
        if (!categoryExists)
        {
            throw new NotFoundException($"Procedure category {command.ProcedureCategoryId} not found.");
        }

        if (command.CodeSourceId is { } codeSourceId)
        {
            bool codeSourceExists = await dbContext.CodeSources
                .AnyAsync(s => s.Id == codeSourceId, cancellationToken)
                .ConfigureAwait(false);
            if (!codeSourceExists)
            {
                throw new NotFoundException($"Code source {codeSourceId} not found.");
            }
        }

        entity.Update(
            command.Code,
            command.Name,
            command.Description,
            command.ProcedureCategoryId,
            command.CodeSourceId,
            command.MacroText,
            command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
