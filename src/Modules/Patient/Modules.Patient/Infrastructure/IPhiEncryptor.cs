namespace FSH.Modules.Patient.Infrastructure;

/// <summary>
/// Encrypts, decrypts, and hashes PHI (Protected Health Information) fields.
/// Backed by ASP.NET Data Protection so keys rotate and are persisted to Redis.
/// </summary>
public interface IPhiEncryptor
{
    /// <summary>Encrypts a plaintext PHI value for storage. Returns null for null/empty input.</summary>
    string? Encrypt(string? plaintext);

    /// <summary>Decrypts a stored PHI ciphertext. Returns null for null/empty input.</summary>
    string? Decrypt(string? ciphertext);

    /// <summary>
    /// Returns a deterministic HMAC-SHA256 hex string suitable for exact-match lookup.
    /// Never reversible — used for SSN search only.
    /// </summary>
    string? HashForSearch(string? value);
}
