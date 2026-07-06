using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientDocuments;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientDocuments.SearchPatientDocuments;

public sealed class SearchPatientDocumentsQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<SearchPatientDocumentsQuery, PagedResponse<PatientDocumentDto>>
{
    public async ValueTask<PagedResponse<PatientDocumentDto>> Handle(
        SearchPatientDocumentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 100 : query.PageSize;

        IQueryable<Domain.PatientDocument> q = dbContext.PatientDocuments
            .AsNoTracking()
            .Where(x => x.PatientId == query.PatientId);

        if (!query.IncludeDeleted)
        {
            q = q.Where(x => !x.IsDeleted);
        }

        if (query.DocumentTypeId is { } documentTypeId)
        {
            q = q.Where(x => x.DocumentTypeId == documentTypeId);
        }

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<PatientDocumentDto> items = await q
            .OrderByDescending(x => x.UploadedAtUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(d => new PatientDocumentDto(
                d.Id, d.PatientId, d.DocumentTypeId, d.FileName, d.ContentType, d.FileSizeBytes,
                d.Notes, d.UploadedByName, d.UploadedAtUtc, d.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PatientDocumentDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
