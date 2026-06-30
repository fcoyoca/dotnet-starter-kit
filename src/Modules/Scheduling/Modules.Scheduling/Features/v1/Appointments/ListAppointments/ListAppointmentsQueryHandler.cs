using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.ListAppointments;

public sealed class ListAppointmentsQueryHandler(SchedulingDbContext dbContext)
    : IQueryHandler<ListAppointmentsQuery, IReadOnlyList<AppointmentDto>>
{
    public async ValueTask<IReadOnlyList<AppointmentDto>> Handle(ListAppointmentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Overlap test: an appointment intersects [FromUtc, ToUtc) when it starts before the window ends
        // and ends after the window starts.
        IQueryable<Domain.Appointment> q = dbContext.Appointments.AsNoTracking()
            .Where(a => a.ClinicId == query.ClinicId && a.StartUtc < query.ToUtc && a.EndUtc > query.FromUtc);

        if (query.ProviderIds is { Count: > 0 } ids)
        {
            q = q.Where(a => ids.Contains(a.ProviderId));
        }

        return await q.OrderBy(a => a.StartUtc)
            .Select(a => new AppointmentDto(a.Id, a.ClinicId, a.ProviderId, a.PatientId, a.AppointmentTypeId,
                a.StartUtc, a.EndUtc, a.Notes, a.Status.ToString(), a.Cancelled, a.NoShow,
                a.IsReservation, a.ReservationTitle, a.ReservationSeriesId, a.ConfirmedAtUtc,
                a.RescheduledToAppointmentId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
