using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Domain;

namespace Patient.Tests.Domain;

public sealed class PatientReportTests
{
    private static PatientReport NewReport() =>
        PatientReport.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), 1, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), null, null, isNoShow: false);

    [Fact]
    public void Create_Should_DefaultDraft_AndVersionOne()
    {
        var r = NewReport();
        r.WorkflowStatus.ShouldBe(ReportWorkflowStatus.Draft);
        r.Version.ShouldBe(1);
        r.IsSigned.ShouldBeFalse();
        r.Vitals.ShouldNotBeNull();
    }

    [Fact]
    public void SetFieldValues_Should_SkipBlank_AndKeepFilled()
    {
        var r = NewReport();
        r.SetFieldValues([(10, "Subjective text"), (11, "   "), (12, "Plan")]);
        r.FieldValues.Count.ShouldBe(2);
        r.FieldValues.ShouldContain(f => f.ReportFieldId == 10);
        r.FieldValues.ShouldContain(f => f.ReportFieldId == 12);
    }

    [Fact]
    public void SetFieldValues_Update_Should_ReuseExistingRows_AndUpdateText()
    {
        // Regression: re-setting field values on an already-populated report must reconcile in place
        // (keep existing rows' keys → EF issues a real UPDATE) instead of Clear()+re-add, which made
        // EF issue a phantom UPDATE against a fresh client key → DbUpdateConcurrencyException.
        var r = NewReport();
        r.SetFieldValues([(10, "a"), (11, "b")]);
        Guid keptId = r.FieldValues.Single(f => f.ReportFieldId == 10).Id;

        r.SetFieldValues([(10, "a2"), (12, "c")]); // update 10, remove 11, add 12

        var f10 = r.FieldValues.Single(f => f.ReportFieldId == 10);
        f10.Id.ShouldBe(keptId);   // same row reused, NOT re-created
        f10.Text.ShouldBe("a2");   // updated in place
        r.FieldValues.ShouldNotContain(f => f.ReportFieldId == 11); // removed
        r.FieldValues.ShouldContain(f => f.ReportFieldId == 12);    // added
        r.FieldValues.Count.ShouldBe(2);
    }

    [Fact]
    public void SetAssociatedProblems_Update_Should_ReuseExistingRows()
    {
        var r = NewReport();
        Guid p1 = Guid.CreateVersion7(), p2 = Guid.CreateVersion7(), p3 = Guid.CreateVersion7();
        r.SetAssociatedProblems([p1, p2]);
        Guid keptId = r.AssociatedProblems.Single(x => x.ProblemId == p1).Id;

        r.SetAssociatedProblems([p1, p3]); // keep p1, remove p2, add p3

        r.AssociatedProblems.Single(x => x.ProblemId == p1).Id.ShouldBe(keptId);
        r.AssociatedProblems.ShouldNotContain(x => x.ProblemId == p2);
        r.AssociatedProblems.ShouldContain(x => x.ProblemId == p3);
        r.AssociatedProblems.Count.ShouldBe(2);
    }

    [Fact]
    public void Sign_Should_LockEditing()
    {
        var r = NewReport();
        r.Sign("user-1", "Dr. House", "patient/reports/x/sig.png");
        r.IsSigned.ShouldBeTrue();
        r.WorkflowStatus.ShouldBe(ReportWorkflowStatus.Signed);
        Should.Throw<InvalidOperationException>(() => r.SetFieldValues([(1, "edit")]));
        Should.Throw<InvalidOperationException>(() => r.UpdateHeader(DateTime.UtcNow, null, null, false));
    }

    [Fact]
    public void AddAddendum_Should_BeAllowedAfterSign()
    {
        var r = NewReport();
        r.Sign("user-1", "Dr. House", null);
        var a = r.AddAddendum("user-2", "Nurse", "Follow-up note");
        r.Addendums.Count.ShouldBe(1);
        a.Text.ShouldBe("Follow-up note");
    }

    [Fact]
    public void ReviewFlow_Should_TransitionStatuses()
    {
        var r = NewReport();
        r.Sign("user-1", "Dr. House", null);
        var reviewer = Guid.CreateVersion7();
        r.RequestReview("user-1", reviewer);
        r.WorkflowStatus.ShouldBe(ReportWorkflowStatus.ReviewRequested);
        r.ReviewerProviderId.ShouldBe(reviewer);
        r.ReviewSign("user-3", "Dr. Wilson", null);
        r.WorkflowStatus.ShouldBe(ReportWorkflowStatus.Reviewed);
    }
}
