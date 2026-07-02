using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Drugs.GetDrugById;

public sealed class GetDrugByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetDrugByIdQuery, DrugDto>
{
    public async ValueTask<DrugDto> Handle(GetDrugByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        DrugDto? dto = await dbContext.Drugs.AsNoTracking()
            .Where(d => d.Id == query.Id && !d.IsDeleted)
            .Select(d => new DrugDto(
                d.Id, d.Name, d.RxAui, d.RxCui, d.Tty, d.Sab, d.Code,
                d.IsActive, d.CreatedAtUtc, d.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Drug {query.Id} not found.");

        return dto;
    }
}
