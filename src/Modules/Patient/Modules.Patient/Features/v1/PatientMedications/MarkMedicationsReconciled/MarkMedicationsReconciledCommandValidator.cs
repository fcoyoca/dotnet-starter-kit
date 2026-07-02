using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.MarkMedicationsReconciled;

public sealed class MarkMedicationsReconciledCommandValidator : AbstractValidator<MarkMedicationsReconciledCommand>
{
    public MarkMedicationsReconciledCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
    }
}
