using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceCompanies.GetInsuranceCompanyById;

public sealed class GetInsuranceCompanyByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetInsuranceCompanyByIdQuery, InsuranceCompanyDto>
{
    public async ValueTask<InsuranceCompanyDto> Handle(GetInsuranceCompanyByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        InsuranceCompanyDto? dto = await dbContext.InsuranceCompanies
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new InsuranceCompanyDto(
                c.Id, c.Name, c.InsuranceTypeId,
                dbContext.InsuranceTypes.Where(t => t.Id == c.InsuranceTypeId).Select(t => t.Name).FirstOrDefault(),
                c.FormularyTiers, c.Address1, c.Address2, c.City, c.State, c.Zip, c.Phone,
                c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return dto ?? throw new NotFoundException($"Insurance company {query.Id} not found.");
    }
}
