using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.ProcedureCodes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ProcedureCodes.GetProcedureCodeById;

public sealed class GetProcedureCodeByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetProcedureCodeByIdQuery, ProcedureCodeDto>
{
    public async ValueTask<ProcedureCodeDto> Handle(GetProcedureCodeByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ProcedureCodeDto? dto = await (
            from c in dbContext.ProcedureCodes.AsNoTracking().Where(c => c.Id == query.Id)
            join cat in dbContext.ProcedureCategories on c.ProcedureCategoryId equals cat.Id into catJoin
            from cat in catJoin.DefaultIfEmpty()
            join src in dbContext.CodeSources on c.CodeSourceId equals src.Id into srcJoin
            from src in srcJoin.DefaultIfEmpty()
            select new ProcedureCodeDto(
                c.Id, c.Code, c.Name, c.Description,
                c.ProcedureCategoryId, cat != null ? cat.Name : null,
                c.CodeSourceId, src != null ? src.Name : null,
                c.MacroText, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return dto ?? throw new NotFoundException($"Procedure code {query.Id} not found.");
    }
}
