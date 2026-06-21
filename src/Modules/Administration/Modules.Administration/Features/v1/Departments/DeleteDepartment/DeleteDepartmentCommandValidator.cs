using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Departments;

namespace FSH.Modules.Administration.Features.v1.Departments.DeleteDepartment;

public sealed class DeleteDepartmentCommandValidator : AbstractValidator<DeleteDepartmentCommand>
{
    public DeleteDepartmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
