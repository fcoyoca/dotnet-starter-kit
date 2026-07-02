using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.UpdatePatientNote;

public sealed class UpdatePatientNoteCommandValidator : AbstractValidator<UpdatePatientNoteCommand>
{
    public UpdatePatientNoteCommandValidator()
    {
        RuleFor(x => x.NoteId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(8000);
    }
}
