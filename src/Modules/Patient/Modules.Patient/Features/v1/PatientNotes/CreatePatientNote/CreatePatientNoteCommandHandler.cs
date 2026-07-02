using FSH.Framework.Core.Context;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.CreatePatientNote;

public sealed class CreatePatientNoteCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<CreatePatientNoteCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePatientNoteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        PatientNote note = PatientNote.Create(
            command.PatientId,
            command.Name,
            command.Description,
            command.IsMedicalAlert,
            currentUser.GetUserId().ToString(),
            currentUser.Name);

        dbContext.PatientNotes.Add(note);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return note.Id;
    }
}
