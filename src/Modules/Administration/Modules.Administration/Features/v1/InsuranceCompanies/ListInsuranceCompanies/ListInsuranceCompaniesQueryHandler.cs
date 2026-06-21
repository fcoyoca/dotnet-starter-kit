using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceCompanies.ListInsuranceCompanies;

public sealed class ListInsuranceCompaniesQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListInsuranceCompaniesQuery, PagedResponse<InsuranceCompanyDto>>
{
    public async ValueTask<PagedResponse<InsuranceCompanyDto>> Handle(ListInsuranceCompaniesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        IQueryable<Domain.InsuranceCompany> q = dbContext.InsuranceCompanies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(c =>
                EF.Functions.ILike(c.Name, $"%{term}%") ||
                (c.City != null && EF.Functions.ILike(c.City, $"%{term}%")));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(c => c.IsActive == query.IsActive.Value);
        }

        if (query.InsuranceTypeId.HasValue)
        {
            q = q.Where(c => c.InsuranceTypeId == query.InsuranceTypeId.Value);
        }

        bool desc = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        q = (query.SortBy?.ToUpperInvariant()) switch
        {
            "CITY" => desc ? q.OrderByDescending(c => c.City) : q.OrderBy(c => c.City),
            _ => desc ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name),
        };

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        List<InsuranceCompanyDto> items = await q
            .Skip((page - 1) * size)
            .Take(size)
            .Select(c => new InsuranceCompanyDto(
                c.Id, c.Name, c.InsuranceTypeId,
                dbContext.InsuranceTypes.Where(t => t.Id == c.InsuranceTypeId).Select(t => t.Name).FirstOrDefault(),
                c.FormularyTiers, c.Address1, c.Address2, c.City, c.State, c.Zip, c.Phone,
                c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<InsuranceCompanyDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
