using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.UpdatePatientNote;

public sealed class UpdatePatientNoteCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<UpdatePatientNoteCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdatePatientNoteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.PatientNote note = await dbContext.PatientNotes
            .FirstOrDefaultAsync(n => n.Id == command.NoteId && !n.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Note {command.NoteId} not found.");

        note.Update(
            command.Name,
            command.Description,
            command.IsMedicalAlert,
            currentUser.GetUserId().ToString(),
            currentUser.Name);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
