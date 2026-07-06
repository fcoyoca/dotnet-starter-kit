using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientDocuments;

namespace FSH.Modules.Patient.Features.v1.PatientDocuments.UploadPatientDocument;

public sealed class UploadPatientDocumentCommandValidator : AbstractValidator<UploadPatientDocumentCommand>
{
    /// <summary>Legacy BackChart's allowed upload extensions (<c>AllowedFileUploadExtensions</c>).</summary>
    public static readonly IReadOnlyList<string> AllowedExtensions =
        [".pdf", ".doc", ".docx", ".png", ".jpg", ".gif", ".wma", ".jpeg", ".pptx", ".ppt", ".dcm", ".xml"];

    // 10 MB of raw bytes; base64 inflates ~4/3, so cap the encoded payload accordingly.
    public const int MaxFileSizeBytes = 10 * 1024 * 1024;
    private const int MaxBase64Length = (MaxFileSizeBytes / 3 + 1) * 4;

    public UploadPatientDocumentCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(260)
            .Must(HasAllowedExtension)
            .WithMessage($"File type is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}");
        RuleFor(x => x.ContentBase64)
            .NotEmpty()
            .Must(c => c is null || c.Length <= MaxBase64Length)
            .WithMessage($"File exceeds the maximum size of {MaxFileSizeBytes / (1024 * 1024)} MB.");
        RuleFor(x => x.ContentType).MaximumLength(128);
        RuleFor(x => x.Notes).MaximumLength(8000);
    }

    private static bool HasAllowedExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        string extension = Path.GetExtension(fileName);
        return AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
