using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.SearchPatientInsurancePolicies;

public sealed class SearchPatientInsurancePoliciesQueryHandler(PatientDbContext dbContext, IMediator mediator)
    : IQueryHandler<SearchPatientInsurancePoliciesQuery, PagedResponse<PatientInsurancePolicyDto>>
{
    public async ValueTask<PagedResponse<PatientInsurancePolicyDto>> Handle(
        SearchPatientInsurancePoliciesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 100 : query.PageSize;

        IQueryable<Domain.PatientInsurancePolicy> q = dbContext.PatientInsurancePolicies
            .AsNoTracking()
            .Where(x => x.PatientId == query.PatientId);

        if (!query.IncludeInactive)
        {
            q = q.Where(x => x.IsActive);
        }

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        // Active first, then in coordination-of-benefits order (Primary → Secondary → …), which is
        // what the enum's declaration order gives us.
        List<Domain.PatientInsurancePolicy> policies = await q
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Priority)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        (Dictionary<Guid, string> companies, Dictionary<Guid, string> types) =
            await InsuranceLookupNames.ResolveAsync(mediator, cancellationToken).ConfigureAwait(false);

        List<PatientInsurancePolicyDto> items =
            [.. policies.Select(x => x.ToDto(companies.NameOrNull(x.InsuranceCompanyId), types.NameOrNull(x.InsuranceTypeId)))];

        return new PagedResponse<PatientInsurancePolicyDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
