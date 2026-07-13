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

        IQueryable<Domain.ReportField> q = dbContext.ReportFields
            .AsNoTracking()
            .Where(f => f.ReportTypeId == query.ReportTypeId);
        if (!query.IncludeInactive)
        {
            q = q.Where(f => f.IsActive);
        }

        return await q
            .OrderBy(f => f.Category).ThenBy(f => f.DisplayOrder).ThenBy(f => f.Name)
            .Select(f => new ReportFieldDto(
                f.Id, f.ReportTypeId, f.Name, f.Category, f.DisplayOrder, f.IsActive, f.DefaultText))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
