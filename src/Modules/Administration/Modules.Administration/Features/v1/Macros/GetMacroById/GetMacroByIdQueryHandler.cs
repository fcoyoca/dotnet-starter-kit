using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Macros;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Macros.GetMacroById;

public sealed class GetMacroByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetMacroByIdQuery, MacroDto>
{
    public async ValueTask<MacroDto> Handle(GetMacroByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        MacroDto? dto = await (
            from c in dbContext.Macros.AsNoTracking().Where(c => c.Id == query.Id)
            join f in dbContext.ReportFields on c.ReportFieldId equals f.Id into fieldJoin
            from f in fieldJoin.DefaultIfEmpty()
            select new MacroDto(
                c.Id, c.Name, c.Text,
                c.ReportFieldId, f != null ? f.Name : null, f != null ? f.Category : null,
                c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return dto ?? throw new NotFoundException($"Macro {query.Id} not found.");
    }
}
