using FluentValidation;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;

namespace FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.UpdatePatientInsurancePolicy;

public sealed class UpdatePatientInsurancePolicyCommandValidator
    : AbstractValidator<UpdatePatientInsurancePolicyCommand>
{
    public UpdatePatientInsurancePolicyCommandValidator()
    {
        RuleFor(x => x.PolicyId).NotEmpty();
        RuleFor(x => x.InsuranceCompanyId).NotEmpty()
            .WithMessage("An insurance company is required.");
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.SubscriberRelationship).IsInEnum();

        RuleFor(x => x.PolicyNumber).MaximumLength(50);
        RuleFor(x => x.GroupNumber).MaximumLength(50);
        RuleFor(x => x.MemberId).MaximumLength(50);
        RuleFor(x => x.CoPay).GreaterThanOrEqualTo(0).When(x => x.CoPay.HasValue);
        RuleFor(x => x.Deductible).GreaterThanOrEqualTo(0).When(x => x.Deductible.HasValue);

        RuleFor(x => x.ExpirationDate)
            .GreaterThanOrEqualTo(x => x.EffectiveDate!.Value)
            .When(x => x.EffectiveDate.HasValue && x.ExpirationDate.HasValue)
            .WithMessage("The plan expiration date cannot precede the plan effective date.");

        RuleFor(x => x.SubscriberFirstName).MaximumLength(100);
        RuleFor(x => x.SubscriberLastName).MaximumLength(100);
        RuleFor(x => x.SubscriberGender).MaximumLength(10);
        RuleFor(x => x.SubscriberEmployerName).MaximumLength(200);
        RuleFor(x => x.SubscriberAddress1).MaximumLength(200);
        RuleFor(x => x.SubscriberAddress2).MaximumLength(200);
        RuleFor(x => x.SubscriberCity).MaximumLength(100);
        RuleFor(x => x.SubscriberState).MaximumLength(2);
        RuleFor(x => x.SubscriberZipCode).MaximumLength(10);
        RuleFor(x => x.Notes).MaximumLength(1000);

        When(x => x.SubscriberRelationship != SubscriberRelationship.Self, () =>
        {
            RuleFor(x => x.SubscriberFirstName).NotEmpty()
                .WithMessage("The subscriber's first name is required when the subscriber is not the patient.");
            RuleFor(x => x.SubscriberLastName).NotEmpty()
                .WithMessage("The subscriber's last name is required when the subscriber is not the patient.");
            RuleFor(x => x.SubscriberDateOfBirth).NotNull()
                .WithMessage("The subscriber's date of birth is required when the subscriber is not the patient.");
        });
    }
}
