using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientNotes;

public sealed record DeletePatientNoteCommand(Guid NoteId) : ICommand<Unit>;
