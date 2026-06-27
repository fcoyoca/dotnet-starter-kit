using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Diagnostics;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Diagnostics.GetDiagnosticById;

public sealed class GetDiagnosticByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetDiagnosticByIdQuery, DiagnosticDto>
{
    public async ValueTask<DiagnosticDto> Handle(GetDiagnosticByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        DiagnosticDto? dto = await (
            from d in dbContext.Diagnostics.AsNoTracking().Where(d => d.Id == query.Id && !d.IsDeleted)
            join s in dbContext.CodeSources on d.CodeSourceId equals s.Id into sourceJoin
            from s in sourceJoin.DefaultIfEmpty()
            select new DiagnosticDto(
                d.Id, d.Code, d.Description, d.LongDescription, d.CodeSourceId,
                s != null ? s.Name : null, d.IsChiropractic, d.IsBillable, d.IsActive,
                d.CreatedAtUtc, d.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Diagnostic {query.Id} not found.");

        return dto;
    }
}
