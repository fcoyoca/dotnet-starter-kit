using FSH.Framework.Core.Domain;
using FSH.Modules.Patient.Domain.Events;

namespace FSH.Modules.Patient.Domain;

public sealed class Patient : AggregateRoot<Guid>, ISoftDeletable
{
    public string PatientCode { get; private set; } = default!;

    /// <summary>
    /// Surrogate key (<c>pUniqueID</c>) of the source record in the legacy BackChart/BronstonChiro
    /// database. Null for patients created natively in this system. Preserved during migration so
    /// later related-record imports (visits, charts) can be keyed back to the original patient.
    /// </summary>
    public long? LegacyUniqueId { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    public PatientDemographics Demographics { get; private set; } = default!;
    public PatientContact Contact { get; private set; } = default!;
    public PatientPhi PHI { get; private set; } = default!;
    public PatientEmployment? Employment { get; private set; }
    public PatientGuardian? Guardian { get; private set; }
    public PatientNextOfKin? NextOfKin { get; private set; }
    public PatientInsurance? Insurance { get; private set; }

    public bool HasNoKnownProblems { get; private set; }
    public bool HasNoKnownMedications { get; private set; }
    public bool HasNoKnownAllergies { get; private set; }
    public bool ReceivesEmailReminders { get; private set; }
    public DateTime? LastVisitDate { get; private set; }
    public DateTime? NextVisitDate { get; private set; }

    private Patient() { }

    public static Patient Create(
        string patientCode,
        bool isActive,
        PatientDemographics demographics,
        PatientContact contact,
        PatientPhi phi,
        PatientEmployment? employment,
        PatientGuardian? guardian,
        PatientNextOfKin? nextOfKin,
        PatientInsurance? insurance,
        bool hasNoKnownProblems,
        bool hasNoKnownMedications,
        bool hasNoKnownAllergies,
        bool receivesEmailReminders,
        DateTime? lastVisitDate,
        DateTime? nextVisitDate,
        long? legacyUniqueId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patientCode);
        ArgumentNullException.ThrowIfNull(demographics);
        ArgumentNullException.ThrowIfNull(contact);
        ArgumentNullException.ThrowIfNull(phi);

        if (demographics.IsMinor && guardian is null)
        {
            throw new ArgumentException("Guardian information is required for minor patients.", nameof(guardian));
        }

        var patient = new Patient
        {
            Id = Guid.CreateVersion7(),
            PatientCode = patientCode.Trim(),
            LegacyUniqueId = legacyUniqueId,
            IsActive = isActive,
            Demographics = demographics,
            Contact = contact,
            PHI = phi,
            Employment = employment,
            Guardian = guardian,
            NextOfKin = nextOfKin,
            Insurance = insurance,
            HasNoKnownProblems = hasNoKnownProblems,
            HasNoKnownMedications = hasNoKnownMedications,
            HasNoKnownAllergies = hasNoKnownAllergies,
            ReceivesEmailReminders = receivesEmailReminders,
            LastVisitDate = lastVisitDate,
            NextVisitDate = nextVisitDate,
            CreatedAtUtc = DateTime.UtcNow
        };

        patient.AddDomainEvent(DomainEvent.Create<PatientCreatedDomainEvent>(
            (id, now) => new(id, now, patient.Id, patient.PatientCode)));

        return patient;
    }

    public void Update(
        string patientCode,
        bool isActive,
        PatientDemographics demographics,
        PatientContact contact,
        PatientPhi phi,
        PatientEmployment? employment,
        PatientGuardian? guardian,
        PatientNextOfKin? nextOfKin,
        PatientInsurance? insurance,
        bool hasNoKnownProblems,
        bool hasNoKnownMedications,
        bool hasNoKnownAllergies,
        bool receivesEmailReminders,
        DateTime? lastVisitDate,
        DateTime? nextVisitDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patientCode);
        ArgumentNullException.ThrowIfNull(demographics);
        ArgumentNullException.ThrowIfNull(contact);
        ArgumentNullException.ThrowIfNull(phi);

        if (demographics.IsMinor && guardian is null)
        {
            throw new ArgumentException("Guardian information is required for minor patients.", nameof(guardian));
        }

        PatientCode = patientCode.Trim();
        IsActive = isActive;
        Demographics = demographics;
        Contact = contact;
        PHI = phi;
        Employment = employment;
        Guardian = guardian;
        NextOfKin = nextOfKin;
        Insurance = insurance;
        HasNoKnownProblems = hasNoKnownProblems;
        HasNoKnownMedications = hasNoKnownMedications;
        HasNoKnownAllergies = hasNoKnownAllergies;
        ReceivesEmailReminders = receivesEmailReminders;
        LastVisitDate = lastVisitDate;
        NextVisitDate = nextVisitDate;
        UpdatedAtUtc = DateTime.UtcNow;

        AddDomainEvent(DomainEvent.Create<PatientUpdatedDomainEvent>(
            (id, now) => new(id, now, Id, PatientCode)));
    }

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Chart "Set No Allergies" checkbox; auto-cleared when an active allergy is saved.</summary>
    public void SetNoKnownAllergies(bool value)
    {
        HasNoKnownAllergies = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Chart "Set No Medications" checkbox; auto-cleared when an active medication is saved.</summary>
    public void SetNoKnownMedications(bool value)
    {
        HasNoKnownMedications = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
