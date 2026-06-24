namespace FSH.Modules.Scheduling.Domain;

/// <summary>Lifecycle state of an appointment. Cancellation / no-show are orthogonal flags.</summary>
public enum AppointmentStatus
{
    Scheduled,
    CheckedIn,
    CheckedOut,
}
