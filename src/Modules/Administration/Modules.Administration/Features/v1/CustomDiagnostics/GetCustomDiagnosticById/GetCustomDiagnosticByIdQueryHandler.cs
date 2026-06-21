using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.GetCustomDiagnosticById;

public sealed class GetCustomDiagnosticByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetCustomDiagnosticByIdQuery, CustomDiagnosticDto>
{
    public async ValueTask<CustomDiagnosticDto> Handle(GetCustomDiagnosticByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        CustomDiagnosticDto? dto = await dbContext.CustomDiagnostics
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new CustomDiagnosticDto(
                c.Id, c.Code, c.Description, c.LongDescription, c.IsChiropractic,
                c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return dto ?? throw new NotFoundException($"Custom diagnostic {query.Id} not found.");
    }
}
