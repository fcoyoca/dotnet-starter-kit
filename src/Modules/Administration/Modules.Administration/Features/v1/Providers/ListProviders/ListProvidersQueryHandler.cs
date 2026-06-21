using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Providers.ListProviders;

public sealed class ListProvidersQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListProvidersQuery, PagedResponse<ProviderDto>>
{
    public async ValueTask<PagedResponse<ProviderDto>> Handle(ListProvidersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        IQueryable<Domain.Provider> q = dbContext.Providers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(p =>
                EF.Functions.ILike(p.FirstName, $"%{term}%") ||
                EF.Functions.ILike(p.LastName, $"%{term}%") ||
                (p.Npi != null && EF.Functions.ILike(p.Npi, $"%{term}%")) ||
                (p.Specialty != null && EF.Functions.ILike(p.Specialty, $"%{term}%")));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(p => p.IsActive == query.IsActive.Value);
        }

        if (query.PrimaryClinicId.HasValue)
        {
            q = q.Where(p => p.PrimaryClinicId == query.PrimaryClinicId.Value);
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        List<ProviderDto> items = await q
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => new ProviderDto(
                p.Id, p.FirstName, p.LastName, p.Prefix, p.Suffix, p.Specialty,
                p.Npi, p.KareoExternalId, p.PrimaryClinicId,
                dbContext.Clinics.Where(c => c.Id == p.PrimaryClinicId).Select(c => c.Name).FirstOrDefault(),
                p.UserId, p.IsActive, p.CreatedAtUtc, p.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<ProviderDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }

    private static IQueryable<Domain.Provider> ApplySort(IQueryable<Domain.Provider> q, string? sortBy, string? sortDir)
    {
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToUpperInvariant()) switch
        {
            "FIRSTNAME" => desc ? q.OrderByDescending(p => p.FirstName) : q.OrderBy(p => p.FirstName),
            "SPECIALTY" => desc ? q.OrderByDescending(p => p.Specialty) : q.OrderBy(p => p.Specialty),
            _ => desc
                ? q.OrderByDescending(p => p.LastName).ThenByDescending(p => p.FirstName)
                : q.OrderBy(p => p.LastName).ThenBy(p => p.FirstName),
        };
    }
}
