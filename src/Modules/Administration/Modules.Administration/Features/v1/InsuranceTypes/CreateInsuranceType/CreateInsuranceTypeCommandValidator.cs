using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.CreateInsuranceType;

public sealed class CreateInsuranceTypeCommandValidator : AbstractValidator<CreateInsuranceTypeCommand>
{
    public CreateInsuranceTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}
