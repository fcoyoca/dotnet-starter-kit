using FSH.Modules.Claims.Contracts.Dtos;

namespace FSH.Modules.Claims.Submission;

/// <summary>
/// Placeholder submitter for this slice: does not transmit anywhere; returns a synthetic control
/// number so the worklist can show a "Submitted" claim. Replace per-tenant with a real clearinghouse
/// submitter behind <see cref="IClaimSubmitter"/>.
/// </summary>
public sealed class StubClaimSubmitter : IClaimSubmitter
{
    public ValueTask<string> SubmitAsync(ClaimDetailDto claim, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(claim);
        var control = $"STUB-{claim.Id.ToString("N")[..8].ToUpperInvariant()}";
        return ValueTask.FromResult(control);
    }
}
