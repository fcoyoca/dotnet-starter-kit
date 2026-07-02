using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.UpdatePatientAllergy;

public sealed class UpdatePatientAllergyCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<UpdatePatientAllergyCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdatePatientAllergyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.PatientAllergy allergy = await dbContext.PatientAllergies
            .FirstOrDefaultAsync(a => a.Id == command.AllergyId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Allergy {command.AllergyId} not found.");

        allergy.Update(
            command.DrugName,
            command.RxAui,
            command.Reaction,
            command.Comments,
            command.DateNoted,
            command.IsActive,
            currentUser.GetUserId().ToString(),
            currentUser.Name);

        if (command.IsActive)
        {
            Domain.Patient? patient = await dbContext.Patients
                .FirstOrDefaultAsync(p => p.Id == allergy.PatientId, cancellationToken)
                .ConfigureAwait(false);
            if (patient is not null && patient.HasNoKnownAllergies)
            {
                patient.SetNoKnownAllergies(false);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
