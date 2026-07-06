using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientDocuments;

namespace FSH.Modules.Patient.Features.v1.PatientDocuments.DeletePatientDocument;

public sealed class DeletePatientDocumentCommandValidator : AbstractValidator<DeletePatientDocumentCommand>
{
    public DeletePatientDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
    }
}
