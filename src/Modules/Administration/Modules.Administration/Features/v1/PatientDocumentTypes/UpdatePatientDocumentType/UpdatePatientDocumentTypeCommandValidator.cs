using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;

namespace FSH.Modules.Administration.Features.v1.PatientDocumentTypes.UpdatePatientDocumentType;

public sealed class UpdatePatientDocumentTypeCommandValidator : AbstractValidator<UpdatePatientDocumentTypeCommand>
{
    public UpdatePatientDocumentTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
