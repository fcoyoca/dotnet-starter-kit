using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientNotes;

public sealed record UpdatePatientNoteCommand(
    Guid NoteId,
    string Name,
    string? Description,
    bool IsMedicalAlert) : ICommand<Unit>;
