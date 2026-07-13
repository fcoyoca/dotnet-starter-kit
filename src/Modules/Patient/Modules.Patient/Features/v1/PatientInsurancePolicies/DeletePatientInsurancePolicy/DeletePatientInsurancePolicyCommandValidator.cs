using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;

namespace FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.DeletePatientInsurancePolicy;

public sealed class DeletePatientInsurancePolicyCommandValidator
    : AbstractValidator<DeletePatientInsurancePolicyCommand>
{
    public DeletePatientInsurancePolicyCommandValidator()
    {
        RuleFor(x => x.PolicyId).NotEmpty();
    }
}
