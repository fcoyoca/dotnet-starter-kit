using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.ListPatientAppointments;

public sealed class ListPatientAppointmentsQueryHandler(SchedulingDbContext dbContext)
    : IQueryHandler<ListPatientAppointmentsQuery, IReadOnlyList<AppointmentDto>>
{
    public async ValueTask<IReadOnlyList<AppointmentDto>> Handle(ListPatientAppointmentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await dbContext.Appointments.AsNoTracking()
            .Where(a => a.PatientId == query.PatientId && !a.Cancelled && !a.IsReservation)
            .OrderByDescending(a => a.StartUtc)
            .Select(a => new AppointmentDto(a.Id, a.ClinicId, a.ProviderId, a.PatientId, a.AppointmentTypeId,
                a.StartUtc, a.EndUtc, a.Notes, a.Status.ToString(), a.Cancelled, a.NoShow,
                a.IsReservation, a.ReservationTitle))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
