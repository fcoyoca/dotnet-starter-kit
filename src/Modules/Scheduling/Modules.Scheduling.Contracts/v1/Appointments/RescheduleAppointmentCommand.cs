using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Appointments;

/// <summary>
/// Reschedules an appointment by creating a replacement at the new provider/time (copying patient/type/notes) and
/// linking the original to it. Returns the new appointment's id. Mirrors BackChart's new-and-link semantics so the
/// original slot is preserved for history.
/// </summary>
public sealed record RescheduleAppointmentCommand(
    Guid Id,
    Guid ProviderId,
    DateTime StartUtc,
    DateTime EndUtc) : ICommand<Guid>;
