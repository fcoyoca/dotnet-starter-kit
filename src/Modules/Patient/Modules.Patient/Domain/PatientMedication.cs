using FSH.Framework.Core.Domain;

namespace FSH.Modules.Patient.Domain;

/// <summary>
/// A patient's medication-list entry (legacy <c>PatientMedications</c>). Drug identity comes from the
/// Administration drug catalog (or free text): <c>RxAui</c> (legacy <c>pmRXAUI</c>), <c>RxCode</c>
/// (RXCUI — drives the MedlinePlus "Info" link), <c>Ndc</c>. Dose is split value/unit/period the way
/// legacy stored it (<c>pmDoseValue</c>/<c>pmDoseUnitID</c>/<c>pmDosePeriodValue</c>/<c>pmDosePeriodUnit</c>);
/// <c>DoseUnitId</c> is a bare cross-schema id into the Administration MedicationDoseUnits lookup.
/// Active/Inactive instead of delete, matching legacy.
/// </summary>
public sealed class PatientMedication : AggregateRoot<Guid>
{
    public Guid PatientId { get; private set; }
    public string DrugName { get; private set; } = default!;
    public string? RxAui { get; private set; }
    public string? RxCode { get; private set; }
    public string? Ndc { get; private set; }
    public string? Prescriber { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public decimal? DoseValue { get; private set; }
    public int? DoseUnitId { get; private set; }
    public decimal? DosePeriodValue { get; private set; }
    public string? DosePeriodUnit { get; private set; }
    public string? Instructions { get; private set; }
    public string? Indication { get; private set; }
    public bool IsActive { get; private set; }

    public string? CreatedByUserId { get; private set; }
    public string? CreatedByName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? UpdatedByUserId { get; private set; }
    public string? UpdatedByName { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private PatientMedication() { }

    public static PatientMedication Create(
        Guid patientId,
        string drugName,
        string? rxAui,
        string? rxCode,
        string? ndc,
        string? prescriber,
        DateTime startDate,
        DateTime? endDate,
        decimal? doseValue,
        int? doseUnitId,
        decimal? dosePeriodValue,
        string? dosePeriodUnit,
        string? instructions,
        string? indication,
        bool isActive,
        string? createdByUserId,
        string? createdByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drugName);
        return new PatientMedication
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            DrugName = drugName.Trim(),
            RxAui = Clean(rxAui),
            RxCode = Clean(rxCode),
            Ndc = Clean(ndc),
            Prescriber = Clean(prescriber),
            StartDate = startDate,
            EndDate = endDate,
            DoseValue = doseValue,
            DoseUnitId = doseUnitId,
            DosePeriodValue = dosePeriodValue,
            DosePeriodUnit = Clean(dosePeriodUnit),
            Instructions = Clean(instructions),
            Indication = Clean(indication),
            IsActive = isActive,
            CreatedByUserId = createdByUserId,
            CreatedByName = createdByName,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string drugName,
        string? rxAui,
        string? rxCode,
        string? ndc,
        string? prescriber,
        DateTime startDate,
        DateTime? endDate,
        decimal? doseValue,
        int? doseUnitId,
        decimal? dosePeriodValue,
        string? dosePeriodUnit,
        string? instructions,
        string? indication,
        bool isActive,
        string? updatedByUserId,
        string? updatedByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drugName);
        DrugName = drugName.Trim();
        RxAui = Clean(rxAui);
        RxCode = Clean(rxCode);
        Ndc = Clean(ndc);
        Prescriber = Clean(prescriber);
        StartDate = startDate;
        EndDate = endDate;
        DoseValue = doseValue;
        DoseUnitId = doseUnitId;
        DosePeriodValue = dosePeriodValue;
        DosePeriodUnit = Clean(dosePeriodUnit);
        Instructions = Clean(instructions);
        Indication = Clean(indication);
        IsActive = isActive;
        UpdatedByUserId = updatedByUserId;
        UpdatedByName = updatedByName;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
