using FluentValidation.TestHelper;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Features.v1.PatientReports.UpdatePatientReport;

namespace Patient.Tests.Validators;

public sealed class UpdatePatientReportCommandValidatorTests
{
    private readonly UpdatePatientReportCommandValidator _sut = new();

    private static ReportVitalsDto SomeVitals() => new(
        HeightInches: null,
        WeightLbs: null,
        Bmi: null,
        Systolic: null,
        Diastolic: null,
        Pulse: null,
        TemperatureF: null);

    private static UpdatePatientReportCommand Valid() => new(
        ReportId: Guid.NewGuid(),
        ReportDate: DateTime.UtcNow,
        ProviderId: null,
        ClinicId: null,
        IsNoShow: false,
        Vitals: SomeVitals(),
        FieldValues: []);

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_VitalsIsNull()
    {
        _sut.TestValidate(Valid() with { Vitals = null! })
            .ShouldHaveValidationErrorFor(x => x.Vitals);
    }

    [Fact]
    public void Validate_Should_Fail_When_FieldValuesIsNull()
    {
        _sut.TestValidate(Valid() with { FieldValues = null! })
            .ShouldHaveValidationErrorFor(x => x.FieldValues);
    }
}
