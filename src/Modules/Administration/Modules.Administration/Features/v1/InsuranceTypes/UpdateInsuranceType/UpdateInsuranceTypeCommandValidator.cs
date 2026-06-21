using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.UpdateInsuranceType;

public sealed class UpdateInsuranceTypeCommandValidator : AbstractValidator<UpdateInsuranceTypeCommand>
{
    public UpdateInsuranceTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}
