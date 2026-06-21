using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.CodeSources;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.CodeSources.UpdateCodeSource;

public sealed class UpdateCodeSourceCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateCodeSourceCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateCodeSourceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.CodeSource entity = await dbContext.CodeSources
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Code source {command.Id} not found.");
        entity.Update(command.Name, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
