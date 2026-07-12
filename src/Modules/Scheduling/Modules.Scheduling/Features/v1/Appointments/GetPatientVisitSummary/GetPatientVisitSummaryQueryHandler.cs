using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.GetPatientVisitSummary;

public sealed class GetPatientVisitSummaryQueryHandler(SchedulingDbContext dbContext)
    : IQueryHandler<GetPatientVisitSummaryQuery, PatientVisitSummaryDto>
{
    public async ValueTask<PatientVisitSummaryDto> Handle(GetPatientVisitSummaryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var now = DateTime.UtcNow;

        // Real patient appointments only — never clinic reservations (patient-less holds).
        var appointments = dbContext.Appointments.AsNoTracking()
            .Where(a => a.PatientId == query.PatientId && !a.IsReservation);

        // Last visit = the most recent *attended* appointment (checked in or out).
        // A cancelled/no-show/never-checked-in slot is not a visit.
        var lastVisit = await appointments
            .Where(a => a.Status == AppointmentStatus.CheckedIn || a.Status == AppointmentStatus.CheckedOut)
            .OrderByDescending(a => a.StartUtc)
            .Select(a => new PatientVisitDto(a.Id, a.StartUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        // Next visit = the earliest still-upcoming appointment that hasn't happened
        // yet (Scheduled, not cancelled/no-show, starting now or later).
        var nextVisit = await appointments
            .Where(a => a.Status == AppointmentStatus.Scheduled
                && !a.Cancelled && !a.NoShow && a.StartUtc >= now)
            .OrderBy(a => a.StartUtc)
            .Select(a => new PatientVisitDto(a.Id, a.StartUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PatientVisitSummaryDto(lastVisit, nextVisit);
    }
}
