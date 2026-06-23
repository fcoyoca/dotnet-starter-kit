using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.AppointmentTypes;

namespace FSH.Modules.Administration.Features.v1.AppointmentTypes.DeleteAppointmentType;

public sealed class DeleteAppointmentTypeCommandValidator : AbstractValidator<DeleteAppointmentTypeCommand>
{
    public DeleteAppointmentTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
