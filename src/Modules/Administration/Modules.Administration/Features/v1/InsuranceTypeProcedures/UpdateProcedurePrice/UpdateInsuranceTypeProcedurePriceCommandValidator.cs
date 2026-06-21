using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.UpdateProcedurePrice;

public sealed class UpdateInsuranceTypeProcedurePriceCommandValidator
    : AbstractValidator<UpdateInsuranceTypeProcedurePriceCommand>
{
    public UpdateInsuranceTypeProcedurePriceCommandValidator()
    {
        RuleFor(x => x.InsuranceTypeId).NotEmpty();
        RuleFor(x => x.ProcedureCodeId).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0m);
    }
}
