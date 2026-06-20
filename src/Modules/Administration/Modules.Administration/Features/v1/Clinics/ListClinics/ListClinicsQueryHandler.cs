using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Clinics;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Clinics.ListClinics;

public sealed class ListClinicsQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListClinicsQuery, PagedResponse<ClinicDto>>
{
    public async ValueTask<PagedResponse<ClinicDto>> Handle(ListClinicsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        IQueryable<Domain.Clinic> q = dbContext.Clinics.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(c =>
                EF.Functions.ILike(c.Name, $"%{term}%") ||
                EF.Functions.ILike(c.Code, $"%{term}%") ||
                EF.Functions.ILike(c.City, $"%{term}%"));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(c => c.IsActive == query.IsActive.Value);
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        List<Domain.Clinic> items = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<ClinicDto>
        {
            Items = items.Select(c => new ClinicDto(
                c.Id, c.Code, c.Name, c.Address1, c.Address2,
                c.City, c.State, c.Zip, c.Phone, c.IsActive,
                c.CreatedAtUtc, c.UpdatedAtUtc)).ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }

    private static IQueryable<Domain.Clinic> ApplySort(IQueryable<Domain.Clinic> q, string? sortBy, string? sortDir)
    {
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToUpperInvariant()) switch
        {
            "CODE" => desc ? q.OrderByDescending(c => c.Code) : q.OrderBy(c => c.Code),
            "CITY" => desc ? q.OrderByDescending(c => c.City) : q.OrderBy(c => c.City),
            "STATE" => desc ? q.OrderByDescending(c => c.State) : q.OrderBy(c => c.State),
            _ => desc ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name),
        };
    }
}
