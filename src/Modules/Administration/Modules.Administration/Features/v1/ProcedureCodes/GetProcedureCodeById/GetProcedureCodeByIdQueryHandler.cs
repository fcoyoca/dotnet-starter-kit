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
        ProcedureCodeDto? dto = await dbContext.ProcedureCodes
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new ProcedureCodeDto(
                c.Id, c.Code, c.Name, c.Description, c.ProcedureCategoryId,
                dbContext.ProcedureCategories.Where(p => p.Id == c.ProcedureCategoryId).Select(p => p.Name).FirstOrDefault(),
                c.CodeSource, c.MacroText, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return dto ?? throw new NotFoundException($"Procedure code {query.Id} not found.");
    }
}
