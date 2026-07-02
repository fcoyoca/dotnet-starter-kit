using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.MarkMedicationsReconciled;

public sealed class MarkMedicationsReconciledCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<MarkMedicationsReconciledCommand, Guid>
{
    public async ValueTask<Guid> Handle(MarkMedicationsReconciledCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        _ = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Patient {command.PatientId} not found.");

        MedicationReconciledDate entry = MedicationReconciledDate.Create(
            command.PatientId,
            DateTime.UtcNow,
            currentUser.GetUserId().ToString(),
            currentUser.Name);

        dbContext.MedicationReconciledDates.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entry.Id;
    }
}
