using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.AppointmentTypes;

namespace FSH.Modules.Administration.Features.v1.AppointmentTypes.UpdateAppointmentType;

public sealed class UpdateAppointmentTypeCommandValidator : AbstractValidator<UpdateAppointmentTypeCommand>
{
    public UpdateAppointmentTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Color).MaximumLength(32);
        RuleFor(x => x.DefaultDurationMinutes).InclusiveBetween(1, 1440);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
