using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Providers.GetProviderById;

public sealed class GetProviderByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetProviderByIdQuery, ProviderDto>
{
    public async ValueTask<ProviderDto> Handle(GetProviderByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ProviderDto? dto = await dbContext.Providers
            .AsNoTracking()
            .Where(p => p.Id == query.Id)
            .Select(p => new ProviderDto(
                p.Id, p.FirstName, p.LastName, p.Prefix, p.Suffix, p.Specialty,
                p.Npi, p.KareoExternalId, p.PrimaryClinicId,
                dbContext.Clinics.Where(c => c.Id == p.PrimaryClinicId).Select(c => c.Name).FirstOrDefault(),
                p.UserId, p.IsActive, p.CreatedAtUtc, p.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return dto ?? throw new NotFoundException($"Provider {query.Id} not found.");
    }
}
