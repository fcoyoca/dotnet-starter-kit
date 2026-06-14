namespace FSH.Modules.Patient.Domain;

public sealed class PatientContact
{
    public string? Address1 { get; private set; }
    public string? Address2 { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? ZipCode { get; private set; }
    public string? Phone { get; private set; }
    public string? PhoneExtension { get; private set; }
    public string? CellPhone { get; private set; }
    public string? Email { get; private set; }
    public int? PreferredContactMethodId { get; private set; }

    private PatientContact() { }

    internal static PatientContact Create(
        string? address1, string? address2, string? city, string? state, string? zipCode,
        string? phone, string? phoneExtension, string? cellPhone, string? email,
        int? preferredContactMethodId) =>
        new()
        {
            Address1 = address1?.Trim(),
            Address2 = address2?.Trim(),
            City = city?.Trim(),
            State = state?.Trim(),
            ZipCode = zipCode?.Trim(),
            Phone = phone?.Trim(),
            PhoneExtension = phoneExtension?.Trim(),
            CellPhone = cellPhone?.Trim(),
            Email = email?.Trim(),
            PreferredContactMethodId = preferredContactMethodId
        };

    internal void Update(
        string? address1, string? address2, string? city, string? state, string? zipCode,
        string? phone, string? phoneExtension, string? cellPhone, string? email,
        int? preferredContactMethodId)
    {
        Address1 = address1?.Trim();
        Address2 = address2?.Trim();
        City = city?.Trim();
        State = state?.Trim();
        ZipCode = zipCode?.Trim();
        Phone = phone?.Trim();
        PhoneExtension = phoneExtension?.Trim();
        CellPhone = cellPhone?.Trim();
        Email = email?.Trim();
        PreferredContactMethodId = preferredContactMethodId;
    }
}
