using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.ListInsuranceTypeProcedures;

public sealed class ListInsuranceTypeProceduresQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListInsuranceTypeProceduresQuery, IReadOnlyList<InsuranceTypeProcedureDto>>
{
    public async ValueTask<IReadOnlyList<InsuranceTypeProcedureDto>> Handle(ListInsuranceTypeProceduresQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        List<InsuranceTypeProcedureDto> items = await dbContext.InsuranceTypeProcedures
            .AsNoTracking()
            .Where(x => x.InsuranceTypeId == query.InsuranceTypeId)
            .Join(
                dbContext.ProcedureCodes,
                itp => itp.ProcedureCodeId,
                pc => pc.Id,
                (itp, pc) => new InsuranceTypeProcedureDto(
                    itp.Id,
                    itp.InsuranceTypeId,
                    itp.ProcedureCodeId,
                    pc.Code,
                    pc.Name,
                    dbContext.ProcedureCategories
                        .Where(cat => cat.Id == pc.ProcedureCategoryId)
                        .Select(cat => cat.Name)
                        .FirstOrDefault(),
                    itp.Price,
                    itp.CreatedAtUtc,
                    itp.UpdatedAtUtc))
            .OrderBy(x => x.ProcedureCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return items;
    }
}
