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

        List<InsuranceTypeProcedureDto> items = await (
            from itp in dbContext.InsuranceTypeProcedures.AsNoTracking()
            where itp.InsuranceTypeId == query.InsuranceTypeId
            join pc in dbContext.ProcedureCodes on itp.ProcedureCodeId equals pc.Id
            join cat in dbContext.ProcedureCategories on pc.ProcedureCategoryId equals cat.Id into catJoin
            from cat in catJoin.DefaultIfEmpty()
            orderby pc.Code
            select new InsuranceTypeProcedureDto(
                itp.Id,
                itp.InsuranceTypeId,
                itp.ProcedureCodeId,
                pc.Code,
                pc.Name,
                cat != null ? cat.Name : null,
                itp.Price,
                itp.CreatedAtUtc,
                itp.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return items;
    }
}
