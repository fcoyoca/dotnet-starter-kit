namespace FSH.Modules.Patient.Domain;

public sealed class PatientEmployment
{
    public string? Occupation { get; private set; }
    public string? EmployerName { get; private set; }
    public string? EmployerAddress1 { get; private set; }
    public string? EmployerAddress2 { get; private set; }
    public string? EmployerCity { get; private set; }
    public string? EmployerState { get; private set; }
    public string? EmployerZipCode { get; private set; }
    public string? EmployerPhone { get; private set; }
    public string? EmployerPhoneExtension { get; private set; }

    private PatientEmployment() { }

    internal static PatientEmployment Create(
        string? occupation, string? employerName,
        string? employerAddress1, string? employerAddress2,
        string? employerCity, string? employerState, string? employerZipCode,
        string? employerPhone, string? employerPhoneExtension) =>
        new()
        {
            Occupation = occupation?.Trim(),
            EmployerName = employerName?.Trim(),
            EmployerAddress1 = employerAddress1?.Trim(),
            EmployerAddress2 = employerAddress2?.Trim(),
            EmployerCity = employerCity?.Trim(),
            EmployerState = employerState?.Trim(),
            EmployerZipCode = employerZipCode?.Trim(),
            EmployerPhone = employerPhone?.Trim(),
            EmployerPhoneExtension = employerPhoneExtension?.Trim()
        };

    internal void Update(
        string? occupation, string? employerName,
        string? employerAddress1, string? employerAddress2,
        string? employerCity, string? employerState, string? employerZipCode,
        string? employerPhone, string? employerPhoneExtension)
    {
        Occupation = occupation?.Trim();
        EmployerName = employerName?.Trim();
        EmployerAddress1 = employerAddress1?.Trim();
        EmployerAddress2 = employerAddress2?.Trim();
        EmployerCity = employerCity?.Trim();
        EmployerState = employerState?.Trim();
        EmployerZipCode = employerZipCode?.Trim();
        EmployerPhone = employerPhone?.Trim();
        EmployerPhoneExtension = employerPhoneExtension?.Trim();
    }
}
