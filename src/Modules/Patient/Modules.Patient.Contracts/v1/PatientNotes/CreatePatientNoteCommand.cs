using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientNotes;

public sealed record CreatePatientNoteCommand(
    Guid PatientId,
    string Name,
    string? Description,
    bool IsMedicalAlert) : ICommand<Guid>;
