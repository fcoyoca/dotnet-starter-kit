namespace FSH.Modules.Scheduling.Contracts.Dtos;

/// <summary>A single derived visit: the appointment behind it, the clinic that owns it (whose
/// timezone the dashboard renders the time in), and when it starts (UTC).</summary>
public sealed record PatientVisitDto(Guid AppointmentId, Guid ClinicId, DateTime StartUtc);

/// <summary>A patient's derived visits, computed from their appointments: the most recent
/// attended appointment and the earliest upcoming one (null when there is no match).</summary>
public sealed record PatientVisitSummaryDto(PatientVisitDto? LastVisit, PatientVisitDto? NextVisit);
