namespace FSH.Modules.Patient.Domain;

/// <summary>
/// Holds Protected Health Information fields. Ssn and GuardianSsn are stored
/// encrypted via EF Core value converters backed by ASP.NET Data Protection.
/// SsnSearchHash is a deterministic HMAC-SHA256 used for exact-match lookup only.
/// </summary>
public sealed class PatientPhi
{
    public string? Ssn { get; private set; }
    public string? SsnSearchHash { get; private set; }
    public string? GuardianSsn { get; private set; }

    private PatientPhi() { }

    internal static PatientPhi Create(
        string? encryptedSsn, string? ssnSearchHash, string? encryptedGuardianSsn) =>
        new()
        {
            Ssn = encryptedSsn,
            SsnSearchHash = ssnSearchHash,
            GuardianSsn = encryptedGuardianSsn
        };

    internal void Update(string? encryptedSsn, string? ssnSearchHash, string? encryptedGuardianSsn)
    {
        Ssn = encryptedSsn;
        SsnSearchHash = ssnSearchHash;
        GuardianSsn = encryptedGuardianSsn;
    }
}
