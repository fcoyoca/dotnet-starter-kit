using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.UpdatePatientInsurancePolicy;

public sealed class UpdatePatientInsurancePolicyCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<UpdatePatientInsurancePolicyCommand, Unit>
{
    public async ValueTask<Unit> Handle(
        UpdatePatientInsurancePolicyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.PatientInsurancePolicy policy = await dbContext.PatientInsurancePolicies
            .FirstOrDefaultAsync(x => x.Id == command.PolicyId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Insurance policy {command.PolicyId} not found.");

        // Coordination of benefits: only one active policy may hold a given priority — ignoring this one.
        if (command.IsActive)
        {
            bool priorityTaken = await dbContext.PatientInsurancePolicies
                .AnyAsync(
                    x => x.PatientId == policy.PatientId
                         && x.Id != policy.Id
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

        // The API only ever echoes the subscriber's SSN back masked, so the edit form's SSN field is
        // blank unless the user retypes it. Treat blank as "leave unchanged" rather than wiping the
        // stored value — same rule the patient's own SSN follows in UpdatePatientCommandHandler.
        string? subscriberSsn = string.IsNullOrWhiteSpace(command.SubscriberSsn)
            ? policy.SubscriberSsn
            : command.SubscriberSsn;

        policy.Update(
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
            subscriberSsn,
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

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
