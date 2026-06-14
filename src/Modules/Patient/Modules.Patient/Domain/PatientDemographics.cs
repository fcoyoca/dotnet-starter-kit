namespace FSH.Modules.Patient.Domain;

public sealed class PatientDemographics
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? MiddleInitial { get; private set; }
    public DateTime DateOfBirth { get; private set; }
    public string Gender { get; private set; } = default!;
    public string? MaritalStatus { get; private set; }
    public bool IsMinor { get; private set; }
    public int? RaceId { get; private set; }
    public int? EthnicityId { get; private set; }
    public int? LanguageId { get; private set; }
    public int? SmokingStatusId { get; private set; }
    public DateTime? SmokingStartDate { get; private set; }
    public DateTime? SmokingEndDate { get; private set; }
    public string? MedicalAlertNotes { get; private set; }

    private PatientDemographics() { }

    internal static PatientDemographics Create(
        string firstName, string lastName, string? middleInitial,
        DateTime dateOfBirth, string gender, string? maritalStatus, bool isMinor,
        int? raceId, int? ethnicityId, int? languageId,
        int? smokingStatusId, DateTime? smokingStartDate, DateTime? smokingEndDate,
        string? medicalAlertNotes) =>
        new()
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            MiddleInitial = middleInitial?.Trim(),
            DateOfBirth = dateOfBirth,
            Gender = gender.Trim(),
            MaritalStatus = maritalStatus?.Trim(),
            IsMinor = isMinor,
            RaceId = raceId,
            EthnicityId = ethnicityId,
            LanguageId = languageId,
            SmokingStatusId = smokingStatusId,
            SmokingStartDate = smokingStartDate,
            SmokingEndDate = smokingEndDate,
            MedicalAlertNotes = medicalAlertNotes?.Trim()
        };

    internal void Update(
        string firstName, string lastName, string? middleInitial,
        DateTime dateOfBirth, string gender, string? maritalStatus, bool isMinor,
        int? raceId, int? ethnicityId, int? languageId,
        int? smokingStatusId, DateTime? smokingStartDate, DateTime? smokingEndDate,
        string? medicalAlertNotes)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        MiddleInitial = middleInitial?.Trim();
        DateOfBirth = dateOfBirth;
        Gender = gender.Trim();
        MaritalStatus = maritalStatus?.Trim();
        IsMinor = isMinor;
        RaceId = raceId;
        EthnicityId = ethnicityId;
        LanguageId = languageId;
        SmokingStatusId = smokingStatusId;
        SmokingStartDate = smokingStartDate;
        SmokingEndDate = smokingEndDate;
        MedicalAlertNotes = medicalAlertNotes?.Trim();
    }
}
