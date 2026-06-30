using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Appointments;

/// <summary>Lists a single patient's real appointments (newest first), for the report
/// "Select Appointment" picker. Excludes cancelled appointments and clinic reservations.</summary>
public sealed record ListPatientAppointmentsQuery(Guid PatientId) : IQuery<IReadOnlyList<AppointmentDto>>;
