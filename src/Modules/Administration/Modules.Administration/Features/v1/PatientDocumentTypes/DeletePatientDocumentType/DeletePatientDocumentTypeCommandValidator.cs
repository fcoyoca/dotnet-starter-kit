using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;

namespace FSH.Modules.Administration.Features.v1.PatientDocumentTypes.DeletePatientDocumentType;

public sealed class DeletePatientDocumentTypeCommandValidator : AbstractValidator<DeletePatientDocumentTypeCommand>
{
    public DeletePatientDocumentTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
