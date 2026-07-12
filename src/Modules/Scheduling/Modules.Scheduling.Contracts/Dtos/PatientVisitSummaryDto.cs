namespace FSH.Modules.Scheduling.Contracts.Dtos;

/// <summary>A single derived visit: the appointment behind it and when it starts (UTC).</summary>
public sealed record PatientVisitDto(Guid AppointmentId, DateTime StartUtc);

/// <summary>A patient's derived visits, computed from their appointments: the most recent
/// attended appointment and the earliest upcoming one (null when there is no match).</summary>
public sealed record PatientVisitSummaryDto(PatientVisitDto? LastVisit, PatientVisitDto? NextVisit);
