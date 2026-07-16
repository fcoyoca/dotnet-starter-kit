using FSH.Modules.Patient.Contracts.v1.SuperBills;
using FSH.Modules.Patient.Domain;
using Shouldly;
using Xunit;

namespace Patient.Tests.Domain;

public sealed class SuperBillTests
{
    [Fact]
    public void Create_Should_Initialize_Unbilled_With_Empty_Procedures()
    {
        Guid reportId = Guid.NewGuid();
        Guid patientId = Guid.NewGuid();

        SuperBill bill = SuperBill.Create(reportId, patientId);

        bill.Id.ShouldNotBe(Guid.Empty);
        bill.ReportId.ShouldBe(reportId);
        bill.PatientId.ShouldBe(patientId);
        bill.IsBilled.ShouldBeFalse();
        bill.BilledDateUtc.ShouldBeNull();
        bill.Procedures.ShouldBeEmpty();
    }

    [Fact]
    public void ReplaceProcedures_Should_Replace_Set_And_Assign_DisplayOrder()
    {
        SuperBill bill = SuperBill.Create(Guid.NewGuid(), Guid.NewGuid());
        Guid dx1 = Guid.NewGuid();
        Guid dx2 = Guid.NewGuid();
        Guid pc1 = Guid.NewGuid();
        Guid pc2 = Guid.NewGuid();

        bill.ReplaceProcedures(
        [
            new ReportProcedureItem(pc1, "98940", "One to two spinal regions", 20.00m, [dx1, dx2]),
        ]);
        bill.ReplaceProcedures(
        [
            new ReportProcedureItem(pc2, "97110", "Therapeutic exercises", 35.50m, [dx1]),
            new ReportProcedureItem(pc2, "97110", "Therapeutic exercises", 35.50m, [dx2]),
        ]);

        bill.Procedures.Count.ShouldBe(2); // duplicates of the same code allowed (legacy)
        bill.Procedures[0].DisplayOrder.ShouldBe(0);
        bill.Procedures[1].DisplayOrder.ShouldBe(1);
        bill.Procedures[0].ProcedureCodeId.ShouldBe(pc2);
        bill.Procedures[0].Code.ShouldBe("97110");
        bill.Procedures[0].Charge.ShouldBe(35.50m);
        bill.Procedures[0].Diagnostics.Single().DiagnosticId.ShouldBe(dx1);
        bill.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void ReplaceProcedures_Update_Should_ReuseOverlappingRows_AndTrimSurplus()
    {
        // Regression: replacing procedures on a populated bill must reconcile in place (keep the
        // overlapping row's key → real EF UPDATE) rather than Clear()+re-add, which triggered a
        // phantom UPDATE against a fresh client key → DbUpdateConcurrencyException.
        SuperBill bill = SuperBill.Create(Guid.NewGuid(), Guid.NewGuid());
        Guid pcA = Guid.NewGuid(), pcB = Guid.NewGuid();
        bill.ReplaceProcedures(
        [
            new ReportProcedureItem(pcA, "111", "A", 10m, [Guid.NewGuid()]),
            new ReportProcedureItem(pcB, "222", "B", 20m, []),
        ]);
        Guid keptId = bill.Procedures[0].Id;

        Guid dx = Guid.NewGuid();
        bill.ReplaceProcedures([new ReportProcedureItem(pcB, "333", "C", 30m, [dx])]);

        bill.Procedures.Count.ShouldBe(1);
        bill.Procedures[0].Id.ShouldBe(keptId);   // overlapping row reused, not re-created
        bill.Procedures[0].Code.ShouldBe("333");  // updated in place
        bill.Procedures[0].Charge.ShouldBe(30m);
        bill.Procedures[0].Diagnostics.Single().DiagnosticId.ShouldBe(dx); // nested reconciled
    }

    [Fact]
    public void ProcedureCreate_Should_Clamp_Negative_Charge_And_Dedupe_Diagnostics()
    {
        Guid dx = Guid.NewGuid();

        SuperBillProcedure proc = SuperBillProcedure.Create(
            Guid.NewGuid(), Guid.NewGuid(), "98940", null, -5m, 0, [dx, dx]);

        proc.Charge.ShouldBe(0m);
        proc.Diagnostics.Count.ShouldBe(1);
    }
}
