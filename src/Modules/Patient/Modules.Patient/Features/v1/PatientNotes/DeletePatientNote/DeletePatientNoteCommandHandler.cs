using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.DeletePatientNote;

public sealed class DeletePatientNoteCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<DeletePatientNoteCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeletePatientNoteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.PatientNote note = await dbContext.PatientNotes
            .FirstOrDefaultAsync(n => n.Id == command.NoteId && !n.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Note {command.NoteId} not found.");

        note.Delete(currentUser.Name ?? currentUser.GetUserId().ToString());
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
