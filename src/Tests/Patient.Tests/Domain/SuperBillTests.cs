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
    public void ProcedureCreate_Should_Clamp_Negative_Charge_And_Dedupe_Diagnostics()
    {
        Guid dx = Guid.NewGuid();

        SuperBillProcedure proc = SuperBillProcedure.Create(
            Guid.NewGuid(), Guid.NewGuid(), "98940", null, -5m, 0, [dx, dx]);

        proc.Charge.ShouldBe(0m);
        proc.Diagnostics.Count.ShouldBe(1);
    }
}
