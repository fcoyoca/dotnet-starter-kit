using FSH.Modules.Claims.Contracts;
using FSH.Modules.Claims.Contracts.Dtos;
using FSH.Modules.Claims.Submission;
using Shouldly;
using Xunit;

namespace Claims.Tests.Submission;

public sealed class StubClaimSubmitterTests
{
    [Fact]
    public async Task SubmitAsync_Returns_NonEmpty_Control_Number()
    {
        var claim = new ClaimDetailDto(
            Id: Guid.NewGuid(), SuperBillId: Guid.NewGuid(), ReportId: Guid.NewGuid(),
            PatientId: Guid.NewGuid(), InsuranceTypeId: null, Status: ClaimStatus.Ready,
            TotalCharge: 100m, ControlNumber: null, CreatedAtUtc: DateTime.UtcNow, UpdatedAtUtc: null,
            SubmittedAtUtc: null, ResolvedAtUtc: null, Lines: []);

        var sut = new StubClaimSubmitter();
        var control = await sut.SubmitAsync(claim, CancellationToken.None);

        control.ShouldNotBeNullOrWhiteSpace();
        control.ShouldStartWith("STUB-");
    }
}
