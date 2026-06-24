using FSH.Framework.Core.Domain;

namespace FSH.Modules.Scheduling.Domain;

/// <summary>
/// A booked appointment (legacy <c>Appointments</c>). Tenant-scoped; references Clinic/Provider/AppointmentType
/// (Administration) and Patient (Patient) by id only — no cross-module FKs. Times are UTC instants; the dashboard
/// renders them in the owning clinic's timezone.
/// </summary>
public sealed class Appointment : AggregateRoot<Guid>, ISoftDeletable
{
    public Guid ClinicId { get; private set; }
    public Guid ProviderId { get; private set; }
    public Guid? PatientId { get; private set; }
    public Guid? AppointmentTypeId { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public string? Notes { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public bool Cancelled { get; private set; }
    public bool NoShow { get; private set; }

    /// <summary>Legacy <c>apptID</c> of the source record; null for native records.</summary>
    public int? LegacyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private Appointment() { }

    public static Appointment Create(
        Guid clinicId, Guid providerId, Guid? patientId, Guid? appointmentTypeId,
        DateTime startUtc, DateTime endUtc, string? notes, int? legacyId = null)
    {
        Guard(clinicId, providerId, startUtc, endUtc);
        return new Appointment
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            ProviderId = providerId,
            PatientId = patientId,
            AppointmentTypeId = appointmentTypeId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Notes = Trim(notes),
            Status = AppointmentStatus.Scheduled,
            LegacyId = legacyId,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(Guid clinicId, Guid providerId, Guid? patientId, Guid? appointmentTypeId,
        DateTime startUtc, DateTime endUtc, string? notes)
    {
        Guard(clinicId, providerId, startUtc, endUtc);
        ClinicId = clinicId;
        ProviderId = providerId;
        PatientId = patientId;
        AppointmentTypeId = appointmentTypeId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Notes = Trim(notes);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void CheckIn() { Status = AppointmentStatus.CheckedIn; UpdatedAtUtc = DateTime.UtcNow; }
    public void CheckOut() { Status = AppointmentStatus.CheckedOut; UpdatedAtUtc = DateTime.UtcNow; }
    public void Cancel() { Cancelled = true; UpdatedAtUtc = DateTime.UtcNow; }
    public void MarkNoShow() { NoShow = true; UpdatedAtUtc = DateTime.UtcNow; }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }

    private static void Guard(Guid clinicId, Guid providerId, DateTime startUtc, DateTime endUtc)
    {
        if (clinicId == Guid.Empty)
        {
            throw new ArgumentException("Clinic is required.", nameof(clinicId));
        }

        if (providerId == Guid.Empty)
        {
            throw new ArgumentException("Provider is required.", nameof(providerId));
        }

        if (endUtc <= startUtc)
        {
            throw new ArgumentException("End must be after start.", nameof(endUtc));
        }
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
