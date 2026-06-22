using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Macros;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Macros.DeleteMacro;

public sealed class DeleteMacroCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteMacroCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteMacroCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.Macro entity = await dbContext.Macros
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Macro {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
