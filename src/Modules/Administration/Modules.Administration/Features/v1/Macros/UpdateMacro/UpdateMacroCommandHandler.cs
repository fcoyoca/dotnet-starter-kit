using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Macros;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Macros.UpdateMacro;

public sealed class UpdateMacroCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateMacroCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateMacroCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.Macro entity = await dbContext.Macros
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Macro {command.Id} not found.");

        entity.Update(command.Name, command.Text, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
