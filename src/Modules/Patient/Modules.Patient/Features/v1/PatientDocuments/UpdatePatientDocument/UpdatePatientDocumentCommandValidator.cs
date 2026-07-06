using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientDocuments;

namespace FSH.Modules.Patient.Features.v1.PatientDocuments.UpdatePatientDocument;

public sealed class UpdatePatientDocumentCommandValidator : AbstractValidator<UpdatePatientDocumentCommand>
{
    public UpdatePatientDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(8000);
    }
}
