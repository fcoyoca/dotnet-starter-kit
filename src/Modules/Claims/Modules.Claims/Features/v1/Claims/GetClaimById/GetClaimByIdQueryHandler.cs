using FSH.Framework.Core.Exceptions;
using FSH.Modules.Claims.Contracts.Dtos;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Claims.Features.v1.Claims.GetClaimById;

public sealed class GetClaimByIdQueryHandler(ClaimsDbContext db)
    : IQueryHandler<GetClaimByIdQuery, ClaimDetailDto>
{
    public async ValueTask<ClaimDetailDto> Handle(GetClaimByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var claim = await db.Claims.AsNoTracking().Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == query.ClaimId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Claim {query.ClaimId} not found.");
        return claim.ToDetailDto();
    }
}
