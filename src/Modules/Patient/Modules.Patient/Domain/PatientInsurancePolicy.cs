using FSH.Framework.Core.Domain;
using FSH.Modules.Patient.Contracts.Dtos;

namespace FSH.Modules.Patient.Domain;

/// <summary>
/// One insurance policy held by a patient (legacy <c>PatientInsurance</c>). A patient may hold many,
/// ordered for coordination of benefits by <see cref="Priority"/> — at most one active policy per
/// priority, enforced by the create/update handlers.
///
/// The subscriber ("insured") block is denormalized onto the policy rather than referenced from the
/// patient: when <see cref="SubscriberRelationship"/> is <c>Self</c> the caller copies the patient's
/// own demographics in, so the row stays a self-contained snapshot of what was submitted to the payer
/// even if the patient's demographics later change. <see cref="SubscriberSsn"/> is PHI and is
/// encrypted at rest by a value converter (see <c>PatientInsurancePolicyConfiguration</c>).
///
/// Deactivation (<see cref="IsActive"/>) is the archival path, matching <see cref="PatientAllergy"/>;
/// legacy's separate <c>pinDeleted</c> soft-delete bit is collapsed into an explicit delete endpoint.
/// </summary>
public sealed class PatientInsurancePolicy : AggregateRoot<Guid>
{
    public Guid PatientId { get; private set; }

    /// <summary>Payer — <c>Administration.InsuranceCompany</c>. Unconstrained by design: EF FK
    /// constraints do not cross module boundaries.</summary>
    public Guid InsuranceCompanyId { get; private set; }

    /// <summary>Plan type — <c>Administration.InsuranceType</c>. Defaults from the payer but is
    /// overridable per policy, as in legacy (<c>pinInsuranceTypeID</c> was its own dropdown).</summary>
    public Guid? InsuranceTypeId { get; private set; }

    public InsurancePriority Priority { get; private set; }

    public string? PolicyNumber { get; private set; }
    public string? GroupNumber { get; private set; }

    /// <summary>The member/subscriber ID printed on the card (legacy <c>pinInsuredsID</c>).</summary>
    public string? MemberId { get; private set; }

    public decimal? CoPay { get; private set; }

    /// <summary>Legacy spelled this <c>pinDeductable</c>; corrected here.</summary>
    public decimal? Deductible { get; private set; }

    public DateTime? EffectiveDate { get; private set; }
    public DateTime? ExpirationDate { get; private set; }

    public SubscriberRelationship SubscriberRelationship { get; private set; }
    public string? SubscriberFirstName { get; private set; }
    public string? SubscriberLastName { get; private set; }
    public DateTime? SubscriberDateOfBirth { get; private set; }
    public string? SubscriberGender { get; private set; }
    public string? SubscriberSsn { get; private set; }
    public string? SubscriberEmployerName { get; private set; }
    public string? SubscriberAddress1 { get; private set; }
    public string? SubscriberAddress2 { get; private set; }
    public string? SubscriberCity { get; private set; }
    public string? SubscriberState { get; private set; }
    public string? SubscriberZipCode { get; private set; }

    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    public string? CreatedByUserId { get; private set; }
    public string? CreatedByName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? UpdatedByUserId { get; private set; }
    public string? UpdatedByName { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private PatientInsurancePolicy() { }

    public static PatientInsurancePolicy Create(
        Guid patientId,
        Guid insuranceCompanyId,
        Guid? insuranceTypeId,
        InsurancePriority priority,
        string? policyNumber,
        string? groupNumber,
        string? memberId,
        decimal? coPay,
        decimal? deductible,
        DateTime? effectiveDate,
        DateTime? expirationDate,
        SubscriberRelationship subscriberRelationship,
        string? subscriberFirstName,
        string? subscriberLastName,
        DateTime? subscriberDateOfBirth,
        string? subscriberGender,
        string? subscriberSsn,
        string? subscriberEmployerName,
        string? subscriberAddress1,
        string? subscriberAddress2,
        string? subscriberCity,
        string? subscriberState,
        string? subscriberZipCode,
        string? notes,
        bool isActive,
        string? createdByUserId,
        string? createdByName)
    {
        if (insuranceCompanyId == Guid.Empty)
        {
            throw new ArgumentException("An insurance company is required.", nameof(insuranceCompanyId));
        }

        ValidateDates(effectiveDate, expirationDate);
        ValidateSubscriber(subscriberRelationship, subscriberFirstName, subscriberLastName, subscriberDateOfBirth);

        return new PatientInsurancePolicy
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            InsuranceCompanyId = insuranceCompanyId,
            InsuranceTypeId = insuranceTypeId,
            Priority = priority,
            PolicyNumber = Clean(policyNumber),
            GroupNumber = Clean(groupNumber),
            MemberId = Clean(memberId),
            CoPay = coPay,
            Deductible = deductible,
            EffectiveDate = effectiveDate,
            ExpirationDate = expirationDate,
            SubscriberRelationship = subscriberRelationship,
            SubscriberFirstName = Clean(subscriberFirstName),
            SubscriberLastName = Clean(subscriberLastName),
            SubscriberDateOfBirth = subscriberDateOfBirth,
            SubscriberGender = Clean(subscriberGender),
            SubscriberSsn = Clean(subscriberSsn),
            SubscriberEmployerName = Clean(subscriberEmployerName),
            SubscriberAddress1 = Clean(subscriberAddress1),
            SubscriberAddress2 = Clean(subscriberAddress2),
            SubscriberCity = Clean(subscriberCity),
            SubscriberState = Clean(subscriberState),
            SubscriberZipCode = Clean(subscriberZipCode),
            Notes = Clean(notes),
            IsActive = isActive,
            CreatedByUserId = createdByUserId,
            CreatedByName = createdByName,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        Guid insuranceCompanyId,
        Guid? insuranceTypeId,
        InsurancePriority priority,
        string? policyNumber,
        string? groupNumber,
        string? memberId,
        decimal? coPay,
        decimal? deductible,
        DateTime? effectiveDate,
        DateTime? expirationDate,
        SubscriberRelationship subscriberRelationship,
        string? subscriberFirstName,
        string? subscriberLastName,
        DateTime? subscriberDateOfBirth,
        string? subscriberGender,
        string? subscriberSsn,
        string? subscriberEmployerName,
        string? subscriberAddress1,
        string? subscriberAddress2,
        string? subscriberCity,
        string? subscriberState,
        string? subscriberZipCode,
        string? notes,
        bool isActive,
        string? updatedByUserId,
        string? updatedByName)
    {
        if (insuranceCompanyId == Guid.Empty)
        {
            throw new ArgumentException("An insurance company is required.", nameof(insuranceCompanyId));
        }

        ValidateDates(effectiveDate, expirationDate);
        ValidateSubscriber(subscriberRelationship, subscriberFirstName, subscriberLastName, subscriberDateOfBirth);

        InsuranceCompanyId = insuranceCompanyId;
        InsuranceTypeId = insuranceTypeId;
        Priority = priority;
        PolicyNumber = Clean(policyNumber);
        GroupNumber = Clean(groupNumber);
        MemberId = Clean(memberId);
        CoPay = coPay;
        Deductible = deductible;
        EffectiveDate = effectiveDate;
        ExpirationDate = expirationDate;
        SubscriberRelationship = subscriberRelationship;
        SubscriberFirstName = Clean(subscriberFirstName);
        SubscriberLastName = Clean(subscriberLastName);
        SubscriberDateOfBirth = subscriberDateOfBirth;
        SubscriberGender = Clean(subscriberGender);
        SubscriberSsn = Clean(subscriberSsn);
        SubscriberEmployerName = Clean(subscriberEmployerName);
        SubscriberAddress1 = Clean(subscriberAddress1);
        SubscriberAddress2 = Clean(subscriberAddress2);
        SubscriberCity = Clean(subscriberCity);
        SubscriberState = Clean(subscriberState);
        SubscriberZipCode = Clean(subscriberZipCode);
        Notes = Clean(notes);
        IsActive = isActive;
        UpdatedByUserId = updatedByUserId;
        UpdatedByName = updatedByName;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Legacy Flex required the insured's name and date of birth whenever the subscriber was someone
    /// other than the patient; the Blazor rewrite dropped the rule. Restored here at the domain level
    /// so it holds regardless of caller.
    /// </summary>
    private static void ValidateSubscriber(
        SubscriberRelationship relationship,
        string? firstName,
        string? lastName,
        DateTime? dateOfBirth)
    {
        if (relationship == SubscriberRelationship.Self)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException(
                "The subscriber's first name is required when the subscriber is not the patient.",
                nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException(
                "The subscriber's last name is required when the subscriber is not the patient.",
                nameof(lastName));
        }

        if (dateOfBirth is null)
        {
            throw new ArgumentException(
                "The subscriber's date of birth is required when the subscriber is not the patient.",
                nameof(dateOfBirth));
        }
    }

    private static void ValidateDates(DateTime? effectiveDate, DateTime? expirationDate)
    {
        if (effectiveDate is not null && expirationDate is not null && expirationDate < effectiveDate)
        {
            throw new ArgumentException(
                "The plan expiration date cannot precede the plan effective date.",
                nameof(expirationDate));
        }
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
