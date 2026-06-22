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

        return await (
            from tf in dbContext.ReportTypeFields.AsNoTracking()
            where tf.ReportTypeId == query.ReportTypeId
            join f in dbContext.ReportFields on tf.ReportFieldId equals f.Id
            where f.IsActive
            orderby f.Category, f.DisplayOrder, f.Name
            select new ReportFieldDto(f.Id, f.Name, f.Category))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
