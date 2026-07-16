using FSH.Framework.Shared.Persistence;
using FSH.Modules.Claims.Contracts.Dtos;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using DomainClaimStatus = FSH.Modules.Claims.Domain.ClaimStatus;

namespace FSH.Modules.Claims.Features.v1.Claims.GetClaims;

public sealed class GetClaimsQueryHandler(ClaimsDbContext db)
    : IQueryHandler<GetClaimsQuery, ClaimsPageDto>
{
    public async ValueTask<ClaimsPageDto> Handle(GetClaimsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Base query (tenant-filtered automatically by BaseDbContext) BEFORE status filter —
        // the summary spans all statuses regardless of the requested status filter/paging.
        var baseQuery = db.Claims.AsNoTracking();
        if (query.InsuranceTypeId is { } payer)
        {
            baseQuery = baseQuery.Where(c => c.InsuranceTypeId == payer);
        }

        // Summary over the (payer-filtered) set, independent of the status filter + paging.
        var counts = await baseQuery
            .GroupBy(c => c.Status)
            .Select(g => new { g.Key, Count = g.Count(), Charge = g.Sum(x => x.TotalCharge) })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        int CountOf(DomainClaimStatus s) => counts.FirstOrDefault(c => c.Key == s)?.Count ?? 0;
        decimal ChargeOf(DomainClaimStatus s) => counts.FirstOrDefault(c => c.Key == s)?.Charge ?? 0m;

        var summary = new ClaimsSummaryDto(
            Draft: CountOf(DomainClaimStatus.Draft),
            Ready: CountOf(DomainClaimStatus.Ready),
            Submitted: CountOf(DomainClaimStatus.Submitted),
            Paid: CountOf(DomainClaimStatus.Paid),
            Denied: CountOf(DomainClaimStatus.Denied),
            OutstandingCharge: ChargeOf(DomainClaimStatus.Ready) + ChargeOf(DomainClaimStatus.Submitted));

        // Rows: apply status filter + paging.
        var rows = baseQuery.Include(c => c.Lines);
        var filtered = query.Status is { } st
            ? rows.Where(c => c.Status == (DomainClaimStatus)(int)st)
            : rows;

        var total = await filtered.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await filtered
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var page = new PagedResponse<ClaimListItemDto>
        {
            Items = items.Select(c => c.ToListItemDto()).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)query.PageSize),
        };

        return new ClaimsPageDto(page, summary);
    }
}
