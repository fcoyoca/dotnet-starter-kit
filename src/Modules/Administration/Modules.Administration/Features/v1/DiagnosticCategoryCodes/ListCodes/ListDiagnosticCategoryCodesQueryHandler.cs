using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategoryCodes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategoryCodes.ListCodes;

public sealed class ListDiagnosticCategoryCodesQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListDiagnosticCategoryCodesQuery, IReadOnlyList<DiagnosticCategoryCodeDto>>
{
    public async ValueTask<IReadOnlyList<DiagnosticCategoryCodeDto>> Handle(ListDiagnosticCategoryCodesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        bool categoryExists = await dbContext.DiagnosticCategories
            .AnyAsync(c => c.Id == query.CategoryId, cancellationToken)
            .ConfigureAwait(false);
        if (!categoryExists)
        {
            throw new NotFoundException($"Diagnostic category {query.CategoryId} not found.");
        }

        List<DiagnosticCategoryCodeDto> items = await (
            from dcc in dbContext.DiagnosticCategoryCodes.AsNoTracking()
            where dcc.DiagnosticCategoryId == query.CategoryId
            join dx in dbContext.Diagnostics on dcc.DiagnosticId equals dx.Id
            orderby dx.Code
            select new DiagnosticCategoryCodeDto(
                dcc.DiagnosticCategoryId,
                dcc.DiagnosticId,
                dx.Code,
                dx.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return items;
    }
}
