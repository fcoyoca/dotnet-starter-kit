using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using FSH.Modules.Patient.Features.v1.PatientIncidents.SetIncidentDiagnostics;
using Shouldly;
using Xunit;

namespace Patient.Tests.Validators;

public sealed class SetIncidentDiagnosticsCommandValidatorTests
{
    private readonly SetIncidentDiagnosticsCommandValidator _sut = new();

    [Fact]
    public void Valid_Command_Should_Pass() =>
        _sut.Validate(new SetIncidentDiagnosticsCommand(Guid.NewGuid(), [Guid.NewGuid()])).IsValid.ShouldBeTrue();

    [Fact]
    public void Empty_DiagnosticIds_List_Should_Pass() =>
        _sut.Validate(new SetIncidentDiagnosticsCommand(Guid.NewGuid(), [])).IsValid.ShouldBeTrue();

    [Fact]
    public void Empty_IncidentId_Should_Fail() =>
        _sut.Validate(new SetIncidentDiagnosticsCommand(Guid.Empty, [])).IsValid.ShouldBeFalse();

    [Fact]
    public void Null_DiagnosticIds_Should_Fail() =>
        _sut.Validate(new SetIncidentDiagnosticsCommand(Guid.NewGuid(), null!)).IsValid.ShouldBeFalse();

    [Fact]
    public void Empty_Diagnostic_Guid_In_List_Should_Fail() =>
        _sut.Validate(new SetIncidentDiagnosticsCommand(Guid.NewGuid(), [Guid.Empty])).IsValid.ShouldBeFalse();
}
