using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.CreatePatientInsurancePolicy;

public sealed class CreatePatientInsurancePolicyCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<CreatePatientInsurancePolicyCommand, Guid>
{
    public async ValueTask<Guid> Handle(
        CreatePatientInsurancePolicyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool patientExists = await dbContext.Patients
            .AnyAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false);
        if (!patientExists)
        {
            throw new NotFoundException($"Patient {command.PatientId} not found.");
        }

        // Coordination of benefits: only one active policy may hold a given priority.
        if (command.IsActive)
        {
            bool priorityTaken = await dbContext.PatientInsurancePolicies
                .AnyAsync(
                    x => x.PatientId == command.PatientId
                         && x.IsActive
                         && x.Priority == command.Priority,
                    cancellationToken)
                .ConfigureAwait(false);
            if (priorityTaken)
            {
                throw new CustomException(
                    $"This patient already has an active {command.Priority} insurance policy. " +
                    "Deactivate it or choose a different priority.");
            }
        }

        PatientInsurancePolicy policy = PatientInsurancePolicy.Create(
            command.PatientId,
            command.InsuranceCompanyId,
            command.InsuranceTypeId,
            command.Priority,
            command.PolicyNumber,
            command.GroupNumber,
            command.MemberId,
            command.CoPay,
            command.Deductible,
            command.EffectiveDate,
            command.ExpirationDate,
            command.SubscriberRelationship,
            command.SubscriberFirstName,
            command.SubscriberLastName,
            command.SubscriberDateOfBirth,
            command.SubscriberGender,
            command.SubscriberSsn,
            command.SubscriberEmployerName,
            command.SubscriberAddress1,
            command.SubscriberAddress2,
            command.SubscriberCity,
            command.SubscriberState,
            command.SubscriberZipCode,
            command.Notes,
            command.IsActive,
            currentUser.GetUserId().ToString(),
            currentUser.Name);

        dbContext.PatientInsurancePolicies.Add(policy);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return policy.Id;
    }
}
