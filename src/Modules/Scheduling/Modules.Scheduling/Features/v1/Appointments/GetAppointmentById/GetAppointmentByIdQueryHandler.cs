using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.GetAppointmentById;

public sealed class GetAppointmentByIdQueryHandler(SchedulingDbContext dbContext)
    : IQueryHandler<GetAppointmentByIdQuery, AppointmentDto>
{
    public async ValueTask<AppointmentDto> Handle(GetAppointmentByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        Domain.Appointment entity = await dbContext.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Appointment {query.Id} not found.");

        return new AppointmentDto(entity.Id, entity.ClinicId, entity.ProviderId, entity.PatientId,
            entity.AppointmentTypeId, entity.StartUtc, entity.EndUtc, entity.Notes, entity.Status.ToString(),
            entity.Cancelled, entity.NoShow, entity.IsReservation, entity.ReservationTitle);
    }
}
