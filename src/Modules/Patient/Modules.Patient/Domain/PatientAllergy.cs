using FSH.Framework.Core.Domain;

namespace FSH.Modules.Patient.Domain;

/// <summary>
/// A patient's allergy-list entry (legacy <c>PatientAllergies</c>). Stores the drug name (free text
/// or picked from the Administration drug catalog, legacy <c>paDrugName</c>/<c>paRXAUI</c>), a
/// reaction string built by the SNOMED reaction picker (legacy <c>paReaction varchar(1000)</c>),
/// comments, the clinician-entered "date noted", and an Active/Inactive status — legacy has no
/// delete for allergies, deactivation is the archival path.
/// </summary>
public sealed class PatientAllergy : AggregateRoot<Guid>
{
    public Guid PatientId { get; private set; }
    public string DrugName { get; private set; } = default!;
    public string? RxAui { get; private set; }
    public string? Reaction { get; private set; }
    public string? Comments { get; private set; }
    public DateTime DateNoted { get; private set; }
    public bool IsActive { get; private set; }

    public string? CreatedByUserId { get; private set; }
    public string? CreatedByName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? UpdatedByUserId { get; private set; }
    public string? UpdatedByName { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private PatientAllergy() { }

    public static PatientAllergy Create(
        Guid patientId,
        string drugName,
        string? rxAui,
        string? reaction,
        string? comments,
        DateTime dateNoted,
        bool isActive,
        string? createdByUserId,
        string? createdByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drugName);
        return new PatientAllergy
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            DrugName = drugName.Trim(),
            RxAui = Clean(rxAui),
            Reaction = Clean(reaction),
            Comments = Clean(comments),
            DateNoted = dateNoted,
            IsActive = isActive,
            CreatedByUserId = createdByUserId,
            CreatedByName = createdByName,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string drugName,
        string? rxAui,
        string? reaction,
        string? comments,
        DateTime dateNoted,
        bool isActive,
        string? updatedByUserId,
        string? updatedByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drugName);
        DrugName = drugName.Trim();
        RxAui = Clean(rxAui);
        Reaction = Clean(reaction);
        Comments = Clean(comments);
        DateNoted = dateNoted;
        IsActive = isActive;
        UpdatedByUserId = updatedByUserId;
        UpdatedByName = updatedByName;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
