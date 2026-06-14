namespace FSH.Modules.Patient.Domain;

public sealed class PatientGuardian
{
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? MiddleInitial { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public string? Gender { get; private set; }
    public string? MaritalStatus { get; private set; }
    public string? Address1 { get; private set; }
    public string? Address2 { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? ZipCode { get; private set; }
    public string? Phone { get; private set; }
    public string? CellPhone { get; private set; }
    public string? EmployerName { get; private set; }
    public string? EmployerAddress1 { get; private set; }
    public string? EmployerAddress2 { get; private set; }
    public string? EmployerCity { get; private set; }
    public string? EmployerState { get; private set; }
    public string? EmployerZipCode { get; private set; }

    private PatientGuardian() { }

    internal static PatientGuardian Create(
        string? firstName, string? lastName, string? middleInitial,
        DateTime? dateOfBirth, string? gender, string? maritalStatus,
        string? address1, string? address2, string? city, string? state, string? zipCode,
        string? phone, string? cellPhone,
        string? employerName, string? employerAddress1, string? employerAddress2,
        string? employerCity, string? employerState, string? employerZipCode) =>
        new()
        {
            FirstName = firstName?.Trim(),
            LastName = lastName?.Trim(),
            MiddleInitial = middleInitial?.Trim(),
            DateOfBirth = dateOfBirth,
            Gender = gender?.Trim(),
            MaritalStatus = maritalStatus?.Trim(),
            Address1 = address1?.Trim(),
            Address2 = address2?.Trim(),
            City = city?.Trim(),
            State = state?.Trim(),
            ZipCode = zipCode?.Trim(),
            Phone = phone?.Trim(),
            CellPhone = cellPhone?.Trim(),
            EmployerName = employerName?.Trim(),
            EmployerAddress1 = employerAddress1?.Trim(),
            EmployerAddress2 = employerAddress2?.Trim(),
            EmployerCity = employerCity?.Trim(),
            EmployerState = employerState?.Trim(),
            EmployerZipCode = employerZipCode?.Trim()
        };

    internal void Update(
        string? firstName, string? lastName, string? middleInitial,
        DateTime? dateOfBirth, string? gender, string? maritalStatus,
        string? address1, string? address2, string? city, string? state, string? zipCode,
        string? phone, string? cellPhone,
        string? employerName, string? employerAddress1, string? employerAddress2,
        string? employerCity, string? employerState, string? employerZipCode)
    {
        FirstName = firstName?.Trim();
        LastName = lastName?.Trim();
        MiddleInitial = middleInitial?.Trim();
        DateOfBirth = dateOfBirth;
        Gender = gender?.Trim();
        MaritalStatus = maritalStatus?.Trim();
        Address1 = address1?.Trim();
        Address2 = address2?.Trim();
        City = city?.Trim();
        State = state?.Trim();
        ZipCode = zipCode?.Trim();
        Phone = phone?.Trim();
        CellPhone = cellPhone?.Trim();
        EmployerName = employerName?.Trim();
        EmployerAddress1 = employerAddress1?.Trim();
        EmployerAddress2 = employerAddress2?.Trim();
        EmployerCity = employerCity?.Trim();
        EmployerState = employerState?.Trim();
        EmployerZipCode = employerZipCode?.Trim();
    }
}
