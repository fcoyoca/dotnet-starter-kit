using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ProcedureCodes;

namespace FSH.Modules.Administration.Features.v1.ProcedureCodes.ListProcedureCodes;

public sealed class ListProcedureCodesQueryValidator : AbstractValidator<ListProcedureCodesQuery>
{
    public ListProcedureCodesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
