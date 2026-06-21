using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.ProcedureCategories;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ProcedureCategories.GetProcedureCategoryById;

public sealed class GetProcedureCategoryByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetProcedureCategoryByIdQuery, ProcedureCategoryDto>
{
    public async ValueTask<ProcedureCategoryDto> Handle(GetProcedureCategoryByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ProcedureCategoryDto? dto = await dbContext.ProcedureCategories
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new ProcedureCategoryDto(
                c.Id, c.Name, c.Description, c.IsImaging, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return dto ?? throw new NotFoundException($"Procedure category {query.Id} not found.");
    }
}
