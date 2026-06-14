namespace FSH.Modules.Patient.Domain;

public sealed class PatientInsurance
{
    public string? InsuredFullName { get; private set; }
    public DateTime? InsuredDateOfBirth { get; private set; }
    public string? InsuredEmployerName { get; private set; }
    public int? ReferralTypeId { get; private set; }

    private PatientInsurance() { }

    internal static PatientInsurance Create(
        string? insuredFullName, DateTime? insuredDateOfBirth,
        string? insuredEmployerName, int? referralTypeId) =>
        new()
        {
            InsuredFullName = insuredFullName?.Trim(),
            InsuredDateOfBirth = insuredDateOfBirth,
            InsuredEmployerName = insuredEmployerName?.Trim(),
            ReferralTypeId = referralTypeId
        };

    internal void Update(
        string? insuredFullName, DateTime? insuredDateOfBirth,
        string? insuredEmployerName, int? referralTypeId)
    {
        InsuredFullName = insuredFullName?.Trim();
        InsuredDateOfBirth = insuredDateOfBirth;
        InsuredEmployerName = insuredEmployerName?.Trim();
        ReferralTypeId = referralTypeId;
    }
}
