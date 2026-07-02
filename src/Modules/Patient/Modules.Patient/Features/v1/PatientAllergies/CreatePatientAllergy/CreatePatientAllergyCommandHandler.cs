using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.CreatePatientAllergy;

public sealed class CreatePatientAllergyCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<CreatePatientAllergyCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePatientAllergyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.Patient patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Patient {command.PatientId} not found.");

        PatientAllergy allergy = PatientAllergy.Create(
            command.PatientId,
            command.DrugName,
            command.RxAui,
            command.Reaction,
            command.Comments,
            command.DateNoted,
            command.IsActive,
            currentUser.GetUserId().ToString(),
            currentUser.Name);

        dbContext.PatientAllergies.Add(allergy);

        // Legacy behavior: saving an active allergy clears the patient's "No Allergies" flag.
        if (command.IsActive && patient.HasNoKnownAllergies)
        {
            patient.SetNoKnownAllergies(false);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return allergy.Id;
    }
}
