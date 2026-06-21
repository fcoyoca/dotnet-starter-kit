using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.CodeSources;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.CodeSources.DeleteCodeSource;

public sealed class DeleteCodeSourceCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteCodeSourceCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteCodeSourceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.CodeSource entity = await dbContext.CodeSources
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Code source {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
