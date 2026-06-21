using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.SetProcedures;

public sealed class SetInsuranceTypeProceduresCommandValidator : AbstractValidator<SetInsuranceTypeProceduresCommand>
{
    public SetInsuranceTypeProceduresCommandValidator()
    {
        RuleFor(x => x.InsuranceTypeId).NotEmpty();
        RuleFor(x => x.Items).NotNull();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProcedureCodeId).NotEmpty();
            item.RuleFor(i => i.Price).GreaterThanOrEqualTo(0);
        });
    }
}
