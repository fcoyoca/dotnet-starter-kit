using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.RemoveProcedure;

public sealed class RemoveProcedureFromInsuranceTypeCommandValidator
    : AbstractValidator<RemoveProcedureFromInsuranceTypeCommand>
{
    public RemoveProcedureFromInsuranceTypeCommandValidator()
    {
        RuleFor(x => x.InsuranceTypeId).NotEmpty();
        RuleFor(x => x.ProcedureCodeId).NotEmpty();
    }
}
