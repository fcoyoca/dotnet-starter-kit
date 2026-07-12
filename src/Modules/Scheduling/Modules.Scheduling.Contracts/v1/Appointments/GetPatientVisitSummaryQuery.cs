using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Appointments;

/// <summary>Derives a patient's last/next visit dates from their appointments.
/// Cross-module read seam: the Patient chart calls this to show visit dates that
/// reflect the schedule instead of a manually-entered column. Excludes cancelled
/// appointments, no-shows, and clinic reservations.</summary>
public sealed record GetPatientVisitSummaryQuery(Guid PatientId) : IQuery<PatientVisitSummaryDto>;
