using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.Patients.SearchPatients;

public sealed class SearchPatientsQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<SearchPatientsQuery, PagedResponse<PatientListItemDto>>
{
    public async ValueTask<PagedResponse<PatientListItemDto>> Handle(
        SearchPatientsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var q = dbContext.Patients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(p =>
                EF.Functions.ILike(p.Demographics.FirstName, $"%{term}%") ||
                EF.Functions.ILike(p.Demographics.LastName, $"%{term}%") ||
                EF.Functions.ILike(p.PatientCode, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.SsnHash))
        {
#pragma warning disable CA1308 // hash comparison is canonical lowercase, not security-sensitive
            string hash = query.SsnHash.Trim().ToLowerInvariant();
#pragma warning restore CA1308
            q = q.Where(p => p.PHI.SsnSearchHash == hash);
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(p => p.IsActive == query.IsActive.Value);
        }

        if (query.ProviderId is { } providerId)
        {
            q = q.Where(p => dbContext.PatientReports.Any(r =>
                r.PatientId == p.Id && !r.IsDeleted && r.ProviderId == providerId));
        }

        if (query.ClinicId is { } clinicId)
        {
            q = q.Where(p => dbContext.PatientReports.Any(r =>
                r.PatientId == p.Id && !r.IsDeleted && r.ClinicId == clinicId));
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var patients = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PatientListItemDto>
        {
            Items = patients.Select(p => new PatientListItemDto(
                p.Id, p.PatientCode,
                p.Demographics.FirstName, p.Demographics.LastName, p.Demographics.MiddleInitial,
                p.Demographics.DateOfBirth, p.Demographics.Gender,
                p.Contact.Email, p.Contact.Phone,
                p.IsActive, p.LastVisitDate,
                p.CreatedAtUtc, p.UpdatedAtUtc)).ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }

    private static IQueryable<Domain.Patient> ApplySort(IQueryable<Domain.Patient> q, string? sortBy, string? sortDir)
    {
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToUpperInvariant()) switch
        {
            "FIRSTNAME" => desc ? q.OrderByDescending(p => p.Demographics.FirstName) : q.OrderBy(p => p.Demographics.FirstName),
            "PATIENTCODE" or "CODE" => desc ? q.OrderByDescending(p => p.PatientCode) : q.OrderBy(p => p.PatientCode),
            "DOB" or "DATEOFBIRTH" => desc ? q.OrderByDescending(p => p.Demographics.DateOfBirth) : q.OrderBy(p => p.Demographics.DateOfBirth),
            "LASTVISIT" => desc ? q.OrderByDescending(p => p.LastVisitDate) : q.OrderBy(p => p.LastVisitDate),
            _ => desc ? q.OrderByDescending(p => p.Demographics.LastName) : q.OrderBy(p => p.Demographics.LastName),
        };
    }
}
