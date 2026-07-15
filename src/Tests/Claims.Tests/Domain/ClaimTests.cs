using FSH.Modules.Claims.Domain;
using Shouldly;
using Xunit;

namespace Claims.Tests.Domain;

public sealed class ClaimTests
{
    private static Claim NewDraft() => Claim.CreateFromSuperBill(
        superBillId: Guid.CreateVersion7(),
        reportId: Guid.CreateVersion7(),
        patientId: Guid.CreateVersion7(),
        insuranceTypeId: Guid.CreateVersion7(),
        isBilled: false,
        lines:
        [
            ClaimLine.Create(Guid.Empty, Guid.CreateVersion7(), "99213", "Office visit", 120m, []),
            ClaimLine.Create(Guid.Empty, Guid.CreateVersion7(), "93000", "EKG", 150m, [Guid.CreateVersion7()]),
        ]);

    [Fact]
    public void CreateFromSuperBill_Starts_Draft_And_Sums_Charges()
    {
        var claim = NewDraft();
        claim.Status.ShouldBe(ClaimStatus.Draft);
        claim.TotalCharge.ShouldBe(270m);
        claim.Lines.Count.ShouldBe(2);
        claim.IsSnapshotEditable.ShouldBeTrue();
    }

    [Fact]
    public void RefreshSnapshot_Replaces_Lines_And_Total_While_Draft()
    {
        var claim = NewDraft();
        claim.RefreshSnapshot(insuranceTypeId: null, isBilled: false,
            lines: [ClaimLine.Create(Guid.Empty, Guid.CreateVersion7(), "36415", "Draw", 15m, [])]);
        claim.Lines.Count.ShouldBe(1);
        claim.TotalCharge.ShouldBe(15m);
        claim.InsuranceTypeId.ShouldBeNull();
    }

    [Fact]
    public void MarkReady_Then_Submit_Sets_ControlNumber_And_Timestamp()
    {
        var claim = NewDraft();
        claim.MarkReady();
        claim.Status.ShouldBe(ClaimStatus.Ready);
        claim.IsSnapshotEditable.ShouldBeFalse();

        claim.Submit("CTRL-123");
        claim.Status.ShouldBe(ClaimStatus.Submitted);
        claim.ControlNumber.ShouldBe("CTRL-123");
        claim.SubmittedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void MarkPaid_From_Submitted_Sets_Resolved()
    {
        var claim = NewDraft();
        claim.MarkReady();
        claim.Submit("C");
        claim.MarkPaid();
        claim.Status.ShouldBe(ClaimStatus.Paid);
        claim.ResolvedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Submit_From_Draft_Throws()
    {
        var claim = NewDraft();
        Should.Throw<InvalidOperationException>(() => claim.Submit("C"));
    }

    [Fact]
    public void MarkPaid_From_Ready_Throws()
    {
        var claim = NewDraft();
        claim.MarkReady();
        Should.Throw<InvalidOperationException>(() => claim.MarkPaid());
    }

    [Fact]
    public void Void_From_Ready_Is_Allowed_But_Void_From_Paid_Throws()
    {
        var claim = NewDraft();
        claim.MarkReady();
        claim.Void();
        claim.Status.ShouldBe(ClaimStatus.Voided);

        var paid = NewDraft();
        paid.MarkReady();
        paid.Submit("C");
        paid.MarkPaid();
        Should.Throw<InvalidOperationException>(() => paid.Void());
    }
}
