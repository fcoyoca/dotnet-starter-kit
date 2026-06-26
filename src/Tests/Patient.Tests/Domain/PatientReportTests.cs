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
