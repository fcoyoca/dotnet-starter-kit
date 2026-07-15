using FSH.Modules.Patient.Contracts.v1.SuperBills;
using FSH.Modules.Patient.Features.v1.SuperBills.SetReportProcedures;
using Shouldly;
using Xunit;

namespace Patient.Tests.Validators;

public sealed class SetReportProceduresCommandValidatorTests
{
    private readonly SetReportProceduresCommandValidator _sut = new();

    private static ReportProcedureItem Item(
        decimal charge = 20m, string code = "98940", Guid? dx = null, Guid? procedureCodeId = null) =>
        new(procedureCodeId ?? Guid.NewGuid(), code, null, charge, [dx ?? Guid.NewGuid()]);

    [Fact]
    public void Valid_Command_Should_Pass() =>
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [Item()])).IsValid.ShouldBeTrue();

    [Fact]
    public void Empty_Procedures_List_Should_Pass() =>
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [])).IsValid.ShouldBeTrue();

    [Fact]
    public void Empty_ReportId_Should_Fail() =>
        _sut.Validate(new SetReportProceduresCommand(Guid.Empty, [])).IsValid.ShouldBeFalse();

    [Fact]
    public void Negative_Charge_Should_Fail() =>
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [Item(charge: -1m)]))
            .IsValid.ShouldBeFalse();

    [Fact]
    public void Blank_Code_Should_Fail() =>
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [Item(code: " ")]))
            .IsValid.ShouldBeFalse();

    [Fact]
    public void Empty_ProcedureCodeId_Should_Fail() =>
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [Item(procedureCodeId: Guid.Empty)]))
            .IsValid.ShouldBeFalse();

    [Fact]
    public void Procedure_Without_Diagnostics_Should_Fail()
    {
        var item = new ReportProcedureItem(Guid.NewGuid(), "98940", null, 20m, []);
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [item])).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Procedure_With_Empty_Diagnostic_Guid_Should_Fail()
    {
        var item = new ReportProcedureItem(Guid.NewGuid(), "98940", null, 20m, [Guid.Empty]);
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [item])).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Null_InsuranceTypeId_Should_Pass() =>
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [Item()], InsuranceTypeId: null))
            .IsValid.ShouldBeTrue();

    [Fact]
    public void Provided_InsuranceTypeId_Should_Pass() =>
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [Item()], InsuranceTypeId: Guid.NewGuid()))
            .IsValid.ShouldBeTrue();

    [Fact]
    public void Empty_InsuranceTypeId_Should_Fail() =>
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [Item()], InsuranceTypeId: Guid.Empty))
            .IsValid.ShouldBeFalse();
}
