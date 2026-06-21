using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Departments;

namespace FSH.Modules.Administration.Features.v1.Departments.ListDepartments;

public sealed class ListDepartmentsQueryValidator : AbstractValidator<ListDepartmentsQuery>
{
    public ListDepartmentsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
