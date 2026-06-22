using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;

namespace FSH.Modules.Administration.Features.v1.PatientDocumentTypes.CreatePatientDocumentType;

public sealed class CreatePatientDocumentTypeCommandValidator : AbstractValidator<CreatePatientDocumentTypeCommand>
{
    public CreatePatientDocumentTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
