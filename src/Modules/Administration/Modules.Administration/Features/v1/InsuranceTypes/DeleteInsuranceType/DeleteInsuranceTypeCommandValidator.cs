using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.DeleteInsuranceType;

public sealed class DeleteInsuranceTypeCommandValidator : AbstractValidator<DeleteInsuranceTypeCommand>
{
    public DeleteInsuranceTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
