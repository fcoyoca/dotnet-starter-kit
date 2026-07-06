using FluentValidation.TestHelper;
using FSH.Modules.Patient.Contracts.v1.PatientDocuments;
using FSH.Modules.Patient.Features.v1.PatientDocuments.UploadPatientDocument;

namespace Patient.Tests.Validators;

public sealed class UploadPatientDocumentCommandValidatorTests
{
    private readonly UploadPatientDocumentCommandValidator _sut = new();

    private static UploadPatientDocumentCommand Valid() => new(
        PatientId: Guid.NewGuid(),
        DocumentTypeId: Guid.NewGuid(),
        FileName: "mri-results.pdf",
        ContentBase64: Convert.ToBase64String([1, 2, 3, 4]),
        ContentType: "application/pdf",
        Notes: "Lumbar MRI");

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_When_DocumentTypeAndNotesAreNull()
    {
        _sut.TestValidate(Valid() with { DocumentTypeId = null, Notes = null })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_PatientIdIsEmpty()
    {
        _sut.TestValidate(Valid() with { PatientId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.PatientId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-extension")]
    [InlineData("script.exe")]
    [InlineData("archive.zip")]
    public void Validate_Should_Fail_For_DisallowedFileName(string fileName)
    {
        _sut.TestValidate(Valid() with { FileName = fileName })
            .ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Theory]
    [InlineData("scan.PDF")]
    [InlineData("photo.JPG")]
    [InlineData("xray.dcm")]
    public void Validate_Should_Allow_LegacyExtensions_CaseInsensitive(string fileName)
    {
        _sut.TestValidate(Valid() with { FileName = fileName })
            .ShouldNotHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void Validate_Should_Fail_When_ContentIsEmpty()
    {
        _sut.TestValidate(Valid() with { ContentBase64 = "" })
            .ShouldHaveValidationErrorFor(x => x.ContentBase64);
    }

    [Fact]
    public void Validate_Should_Fail_When_ContentExceedsMaxSize()
    {
        // Base64 length just past the 10MB-raw cap.
        string oversized = new('A', (UploadPatientDocumentCommandValidator.MaxFileSizeBytes / 3 + 2) * 4);
        _sut.TestValidate(Valid() with { ContentBase64 = oversized })
            .ShouldHaveValidationErrorFor(x => x.ContentBase64);
    }
}
