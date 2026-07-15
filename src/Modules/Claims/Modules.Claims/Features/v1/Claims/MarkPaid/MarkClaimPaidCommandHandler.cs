using FSH.Framework.Core.Exceptions;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Claims.Features.v1.Claims.MarkPaid;

public sealed class MarkClaimPaidCommandHandler(ClaimsDbContext db)
    : ICommandHandler<MarkClaimPaidCommand, Guid>
{
    public async ValueTask<Guid> Handle(MarkClaimPaidCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var claim = await db.Claims.FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken)
            .ConfigureAwait(false) ?? throw new NotFoundException($"Claim {command.ClaimId} not found.");
        claim.MarkPaid();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return claim.Id;
    }
}
