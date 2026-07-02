using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.DeletePatientNote;

public sealed class DeletePatientNoteCommandValidator : AbstractValidator<DeletePatientNoteCommand>
{
    public DeletePatientNoteCommandValidator()
    {
        RuleFor(x => x.NoteId).NotEmpty();
    }
}
