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

    /// <summary>
    /// True when this is a "reserve time" block — a titled hold on a provider's calendar with no patient or
    /// appointment type (legacy <c>IsReserveTime</c>). Patient/type are forced null for reservations.
    /// </summary>
    public bool IsReservation { get; private set; }

    /// <summary>Display title for a reservation block (required when <see cref="IsReservation"/>); null otherwise.</summary>
    public string? ReservationTitle { get; private set; }

    /// <summary>
    /// Groups materialized occurrences of a recurring reserve-time series so the whole series can be operated on
    /// together (e.g. delete-all). Null for one-off appointments and single reservations.
    /// </summary>
    public Guid? ReservationSeriesId { get; private set; }

    /// <summary>When the patient confirmed this appointment (legacy <c>apptReminderConfirmedDate</c>); null = unconfirmed.</summary>
    public DateTime? ConfirmedAtUtc { get; private set; }

    /// <summary>
    /// Id of the replacement appointment this one was rescheduled to (legacy <c>apptRescheduledToID</c>). Non-null means
    /// this slot was moved; it stays for history and is read-only.
    /// </summary>
    public Guid? RescheduledToAppointmentId { get; private set; }

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
        DateTime startUtc, DateTime endUtc, string? notes, int? legacyId = null,
        bool isReservation = false, string? reservationTitle = null, Guid? reservationSeriesId = null)
    {
        Guard(clinicId, providerId, startUtc, endUtc);
        var title = NormalizeReservation(isReservation, reservationTitle);
        return new Appointment
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            ProviderId = providerId,
            PatientId = isReservation ? null : patientId,
            AppointmentTypeId = isReservation ? null : appointmentTypeId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Notes = Trim(notes),
            Status = AppointmentStatus.Scheduled,
            IsReservation = isReservation,
            ReservationTitle = title,
            ReservationSeriesId = isReservation ? reservationSeriesId : null,
            LegacyId = legacyId,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(Guid clinicId, Guid providerId, Guid? patientId, Guid? appointmentTypeId,
        DateTime startUtc, DateTime endUtc, string? notes,
        bool isReservation = false, string? reservationTitle = null)
    {
        Guard(clinicId, providerId, startUtc, endUtc);
        var title = NormalizeReservation(isReservation, reservationTitle);
        ClinicId = clinicId;
        ProviderId = providerId;
        PatientId = isReservation ? null : patientId;
        AppointmentTypeId = isReservation ? null : appointmentTypeId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Notes = Trim(notes);
        IsReservation = isReservation;
        ReservationTitle = title;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void CheckIn() { Status = AppointmentStatus.CheckedIn; UpdatedAtUtc = DateTime.UtcNow; }
    public void CheckOut() { Status = AppointmentStatus.CheckedOut; UpdatedAtUtc = DateTime.UtcNow; }
    public void Cancel() { Cancelled = true; UpdatedAtUtc = DateTime.UtcNow; }
    public void MarkNoShow() { NoShow = true; UpdatedAtUtc = DateTime.UtcNow; }

    /// <summary>Records the patient's confirmation (idempotent — keeps the earliest confirmation timestamp).</summary>
    public void Confirm()
    {
        ConfirmedAtUtc ??= DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Links this slot to the replacement appointment it was rescheduled to.</summary>
    public void MarkRescheduled(Guid newAppointmentId)
    {
        if (newAppointmentId == Guid.Empty)
        {
            throw new ArgumentException("Replacement appointment id is required.", nameof(newAppointmentId));
        }

        RescheduledToAppointmentId = newAppointmentId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

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

    private static string? NormalizeReservation(bool isReservation, string? reservationTitle)
    {
        if (!isReservation)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(reservationTitle))
        {
            throw new ArgumentException("Reservation title is required for a reserve-time block.", nameof(reservationTitle));
        }

        return reservationTitle.Trim();
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
