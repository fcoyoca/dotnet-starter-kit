using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace FSH.Modules.Patient.Infrastructure;

public sealed class PhiEncryptor : IPhiEncryptor
{
    // Stable, deterministic HMAC key derived from the protector purpose string.
    // Using Protect() would produce non-deterministic output (random IV), breaking
    // cross-request/cross-restart hash lookups. Tenant isolation is enforced by
    // EF Core query filters, not at the hash level.
    // Sprint 4: rotate to a secret from Key Vault for defense-in-depth.
    private static readonly byte[] HmacKey =
        SHA256.HashData(Encoding.UTF8.GetBytes("FSH.Patient.PHI.HMAC.v1"));

    private readonly IDataProtector _protector;

    public PhiEncryptor(IDataProtectionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _protector = provider.CreateProtector("FSH.Patient.PHI.v1");
    }

    public string? Encrypt(string? plaintext) =>
        string.IsNullOrEmpty(plaintext) ? null : _protector.Protect(plaintext);

    public string? Decrypt(string? ciphertext) =>
        string.IsNullOrEmpty(ciphertext) ? null : _protector.Unprotect(ciphertext);

    public string? HashForSearch(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        byte[] data = Encoding.UTF8.GetBytes(value.Trim().ToUpperInvariant());
        byte[] hash = HMACSHA256.HashData(HmacKey, data);
#pragma warning disable CA1308 // hex string is canonical lowercase, not security-sensitive
        return Convert.ToHexString(hash).ToLowerInvariant();
#pragma warning restore CA1308
    }
}
