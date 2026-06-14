using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace FSH.Modules.Patient.Infrastructure;

public sealed class PhiEncryptor : IPhiEncryptor
{
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

        // Derive a stable HMAC key from the data protector so the hash is
        // tenant-scoped and rotates with the Data Protection key ring.
        // We protect a fixed sentinel, then use its UTF-8 bytes as the HMAC key.
        byte[] hmacKey = Encoding.UTF8.GetBytes(_protector.Protect("phi-search-key-v1"));
        byte[] data = Encoding.UTF8.GetBytes(value.Trim().ToUpperInvariant());
        byte[] hash = HMACSHA256.HashData(hmacKey, data);
#pragma warning disable CA1308 // hex string is canonical lowercase, not security-sensitive
        return Convert.ToHexString(hash).ToLowerInvariant();
#pragma warning restore CA1308
    }
}
