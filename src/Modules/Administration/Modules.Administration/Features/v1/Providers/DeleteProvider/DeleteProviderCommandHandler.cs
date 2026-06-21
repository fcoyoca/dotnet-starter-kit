using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Providers.DeleteProvider;

public sealed class DeleteProviderCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteProviderCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteProviderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.Provider entity = await dbContext.Providers
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Provider {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
