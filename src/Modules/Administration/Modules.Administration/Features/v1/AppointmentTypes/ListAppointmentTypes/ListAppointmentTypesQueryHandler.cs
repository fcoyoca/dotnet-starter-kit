using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.AppointmentTypes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.AppointmentTypes.ListAppointmentTypes;

public sealed class ListAppointmentTypesQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListAppointmentTypesQuery, IReadOnlyList<AppointmentTypeDto>>
{
    public async ValueTask<IReadOnlyList<AppointmentTypeDto>> Handle(ListAppointmentTypesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        IQueryable<Domain.AppointmentType> q = dbContext.AppointmentTypes.AsNoTracking();
        if (query.IsActive.HasValue)
        {
            q = q.Where(a => a.IsActive == query.IsActive.Value);
        }

        return await q
            .OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name)
            .Select(a => new AppointmentTypeDto(a.Id, a.Name, a.Color, a.DefaultDurationMinutes, a.DisplayOrder, a.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
