using FSH.Framework.Core.Exceptions;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Claims.Features.v1.Claims.Void;

public sealed class VoidClaimCommandHandler(ClaimsDbContext db)
    : ICommandHandler<VoidClaimCommand, Guid>
{
    public async ValueTask<Guid> Handle(VoidClaimCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var claim = await db.Claims.FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken)
            .ConfigureAwait(false) ?? throw new NotFoundException($"Claim {command.ClaimId} not found.");
        claim.Void();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return claim.Id;
    }
}
