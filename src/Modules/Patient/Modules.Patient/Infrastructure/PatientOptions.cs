using System.ComponentModel.DataAnnotations;

namespace FSH.Modules.Patient.Infrastructure;

public sealed class PatientOptions
{
    public const string SectionName = "PatientOptions";

    /// <summary>
    /// Base64-encoded 32-byte HMAC-SHA256 key used to hash SSN values for
    /// exact-match lookup. In production this must come from Key Vault or a
    /// secrets manager — never hard-code in source. Set via the
    /// PatientOptions__PhiHmacKey environment variable or Key Vault binding.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string PhiHmacKey { get; init; } = default!;
}
