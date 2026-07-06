using FluentValidation.TestHelper;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Features.v1.PatientReports.ExportPatientReportsPdf;
using Shouldly;

namespace Patient.Tests.Validators;

public sealed class ExportPatientReportsPdfQueryValidatorTests
{
    private readonly ExportPatientReportsPdfQueryValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_For_ValidQuery()
    {
        _sut.TestValidate(new ExportPatientReportsPdfQuery([Guid.NewGuid(), Guid.NewGuid()]))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_ReportIdsIsEmpty()
    {
        _sut.TestValidate(new ExportPatientReportsPdfQuery([]))
            .ShouldHaveValidationErrorFor(x => x.ReportIds);
    }

    [Fact]
    public void Validate_Should_Fail_When_ReportIdsExceedsLimit()
    {
        var ids = Enumerable.Range(0, ExportPatientReportsPdfQueryValidator.MaxReportsPerExport + 1)
            .Select(_ => Guid.NewGuid())
            .ToList();
        _sut.TestValidate(new ExportPatientReportsPdfQuery(ids))
            .ShouldHaveValidationErrorFor(x => x.ReportIds);
    }

    [Fact]
    public void Validate_Should_Fail_When_AnyReportIdIsEmpty()
    {
        _sut.TestValidate(new ExportPatientReportsPdfQuery([Guid.NewGuid(), Guid.Empty]))
            .Errors.ShouldNotBeEmpty();
    }
}
