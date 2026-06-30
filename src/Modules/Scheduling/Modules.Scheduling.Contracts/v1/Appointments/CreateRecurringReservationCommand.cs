using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Appointments;

/// <summary>A single materialized occurrence of a recurring reserve-time series (UTC instants).</summary>
public sealed record ReservationOccurrence(DateTime StartUtc, DateTime EndUtc);

/// <summary>
/// Creates a recurring reserve-time block as one materialized <c>Appointment</c> per occurrence, grouped by a shared
/// <c>ReservationSeriesId</c>. Occurrences are pre-expanded by the caller in the clinic's local time (weekdays + end
/// date) and supplied as UTC instants. Returns the number of reservation blocks created.
/// </summary>
public sealed record CreateRecurringReservationCommand(
    Guid ClinicId,
    Guid ProviderId,
    string Title,
    string? Notes,
    IReadOnlyList<ReservationOccurrence> Occurrences) : ICommand<int>;
