using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.GetInsuranceTypeById;

public sealed class GetInsuranceTypeByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetInsuranceTypeByIdQuery, InsuranceTypeDto>
{
    public async ValueTask<InsuranceTypeDto> Handle(GetInsuranceTypeByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        InsuranceTypeDto? dto = await dbContext.InsuranceTypes
            .AsNoTracking()
            .Where(t => t.Id == query.Id)
            .Select(t => new InsuranceTypeDto(
                t.Id, t.Name, t.IsActive, t.ProcedureCategoryId,
                dbContext.ProcedureCategories.Where(c => c.Id == t.ProcedureCategoryId).Select(c => c.Name).FirstOrDefault(),
                t.CreatedAtUtc, t.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return dto ?? throw new NotFoundException($"Insurance type {query.Id} not found.");
    }
}
