using FSH.Framework.Core.Exceptions;
using FSH.Framework.Storage.Services;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Providers.ClearProviderSignature;

public sealed class ClearProviderSignatureCommandHandler(AdministrationDbContext dbContext, IStorageService storage)
    : ICommandHandler<ClearProviderSignatureCommand, Unit>
{
    public async ValueTask<Unit> Handle(ClearProviderSignatureCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.Provider entity = await dbContext.Providers
            .FirstOrDefaultAsync(p => p.Id == command.ProviderId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Provider {command.ProviderId} not found.");

        if (entity.SignatureImagePath is { } path)
        {
            await storage.RemoveAsync(path, cancellationToken).ConfigureAwait(false);
            entity.ClearSignature();
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
