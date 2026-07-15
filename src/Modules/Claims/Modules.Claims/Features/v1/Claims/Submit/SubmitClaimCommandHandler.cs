using FSH.Framework.Core.Exceptions;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using FSH.Modules.Claims.Submission;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Claims.Features.v1.Claims.Submit;

public sealed class SubmitClaimCommandHandler(ClaimsDbContext db, IClaimSubmitter submitter)
    : ICommandHandler<SubmitClaimCommand, Guid>
{
    public async ValueTask<Guid> Handle(SubmitClaimCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var claim = await db.Claims.Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Claim {command.ClaimId} not found.");

        var control = await submitter.SubmitAsync(claim.ToDetailDto(), cancellationToken).ConfigureAwait(false);
        claim.Submit(control);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return claim.Id;
    }
}
