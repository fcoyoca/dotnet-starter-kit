using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategories.GetDiagnosticCategoryById;

public sealed class GetDiagnosticCategoryByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetDiagnosticCategoryByIdQuery, DiagnosticCategoryDto>
{
    public async ValueTask<DiagnosticCategoryDto> Handle(GetDiagnosticCategoryByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        DiagnosticCategoryDto? dto = await dbContext.DiagnosticCategories
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new DiagnosticCategoryDto(c.Id, c.Name, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return dto ?? throw new NotFoundException($"Diagnostic category {query.Id} not found.");
    }
}
