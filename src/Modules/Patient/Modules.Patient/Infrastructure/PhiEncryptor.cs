using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Patient.Infrastructure;

public sealed class PhiEncryptor : IPhiEncryptor
{
    private readonly IDataProtector _protector;
    private readonly byte[] _hmacKey;

    public PhiEncryptor(IDataProtectionProvider provider, IOptions<PatientOptions> options)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(options);

        _protector = provider.CreateProtector("FSH.Patient.PHI.v1");

        // Derive a fixed-length key from the configured base64 secret so the hash
        // is stable across restarts, tenant boundaries are enforced by EF query
        // filters (not at hash level), and the secret is rotatable via config.
        byte[] raw = Convert.FromBase64String(options.Value.PhiHmacKey);
        _hmacKey = SHA256.HashData(raw);
    }

    public string? Encrypt(string? plaintext) =>
        string.IsNullOrEmpty(plaintext) ? null : _protector.Protect(plaintext);

    public string? Decrypt(string? ciphertext) =>
        string.IsNullOrEmpty(ciphertext) ? null : _protector.Unprotect(ciphertext);

    public string? HashForSearch(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        byte[] data = Encoding.UTF8.GetBytes(value.Trim().ToUpperInvariant());
        byte[] hash = HMACSHA256.HashData(_hmacKey, data);
#pragma warning disable CA1308 // hex string is canonical lowercase, not security-sensitive
        return Convert.ToHexString(hash).ToLowerInvariant();
#pragma warning restore CA1308
    }
}
