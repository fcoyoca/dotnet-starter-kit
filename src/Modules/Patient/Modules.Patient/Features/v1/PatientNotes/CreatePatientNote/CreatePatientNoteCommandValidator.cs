using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.CreatePatientNote;

public sealed class CreatePatientNoteCommandValidator : AbstractValidator<CreatePatientNoteCommand>
{
    public CreatePatientNoteCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(8000);
    }
}
