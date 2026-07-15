using FSH.Framework.Core.Exceptions;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Claims.Features.v1.Claims.MarkDenied;

public sealed class MarkClaimDeniedCommandHandler(ClaimsDbContext db)
    : ICommandHandler<MarkClaimDeniedCommand, Guid>
{
    public async ValueTask<Guid> Handle(MarkClaimDeniedCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var claim = await db.Claims.FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken)
            .ConfigureAwait(false) ?? throw new NotFoundException($"Claim {command.ClaimId} not found.");
        claim.MarkDenied();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return claim.Id;
    }
}
