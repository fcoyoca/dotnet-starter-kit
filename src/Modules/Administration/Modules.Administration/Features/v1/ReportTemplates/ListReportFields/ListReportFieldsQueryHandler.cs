using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.ListReportFields;

public sealed class ListReportFieldsQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListReportFieldsQuery, IReadOnlyList<ReportFieldDto>>
{
    public async ValueTask<IReadOnlyList<ReportFieldDto>> Handle(ListReportFieldsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await dbContext.ReportFields
            .AsNoTracking()
            .Where(f => f.ReportTypeId == query.ReportTypeId && f.IsActive)
            .OrderBy(f => f.Category).ThenBy(f => f.DisplayOrder).ThenBy(f => f.Name)
            .Select(f => new ReportFieldDto(f.Id, f.Name, f.Category))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
