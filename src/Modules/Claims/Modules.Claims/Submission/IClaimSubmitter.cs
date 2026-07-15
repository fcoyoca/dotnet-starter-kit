using FSH.Modules.Claims.Contracts.Dtos;

namespace FSH.Modules.Claims.Submission;

/// <summary>
/// Outbound seam to a clearinghouse. This slice ships <c>StubClaimSubmitter</c>; real
/// Cvikota/Kareo/OfficeAlly submitters land later behind this same interface.
/// </summary>
public interface IClaimSubmitter
{
    /// <summary>Submits the claim and returns the payer/clearinghouse control number.</summary>
    ValueTask<string> SubmitAsync(ClaimDetailDto claim, CancellationToken ct = default);
}
