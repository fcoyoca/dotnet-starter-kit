using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.PatientDocumentTypes.ListPatientDocumentTypes;

public sealed class ListPatientDocumentTypesQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListPatientDocumentTypesQuery, PagedResponse<PatientDocumentTypeDto>>
{
    public async ValueTask<PagedResponse<PatientDocumentTypeDto>> Handle(ListPatientDocumentTypesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        IQueryable<Domain.PatientDocumentType> q = dbContext.PatientDocumentTypes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(c => EF.Functions.ILike(c.Name, $"%{term}%"));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(c => c.IsActive == query.IsActive.Value);
        }

        bool desc = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        q = desc ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        List<PatientDocumentTypeDto> items = await q
            .Skip((page - 1) * size)
            .Take(size)
            .Select(c => new PatientDocumentTypeDto(c.Id, c.Name, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PatientDocumentTypeDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
