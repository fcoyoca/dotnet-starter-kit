using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.ListReportTypes;

public sealed class ListReportTypesQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListReportTypesQuery, IReadOnlyList<ReportTypeDto>>
{
    public async ValueTask<IReadOnlyList<ReportTypeDto>> Handle(ListReportTypesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await dbContext.ReportTypes
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Select(t => new ReportTypeDto(t.Id, t.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
