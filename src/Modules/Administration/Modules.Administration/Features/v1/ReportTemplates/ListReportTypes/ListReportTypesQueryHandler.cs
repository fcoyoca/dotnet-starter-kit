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
        IQueryable<Domain.ReportType> q = dbContext.ReportTypes.AsNoTracking();
        if (query.IsActive.HasValue)
        {
            q = q.Where(t => t.IsActive == query.IsActive.Value);
        }

        return await q
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Name)
            .Select(t => new ReportTypeDto(t.Id, t.Name, t.DisplayOrder, t.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
