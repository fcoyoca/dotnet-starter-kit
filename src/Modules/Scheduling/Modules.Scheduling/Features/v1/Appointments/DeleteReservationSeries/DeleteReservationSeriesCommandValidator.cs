using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.DeleteReservationSeries;

public sealed class DeleteReservationSeriesCommandValidator : AbstractValidator<DeleteReservationSeriesCommand>
{
    public DeleteReservationSeriesCommandValidator() => RuleFor(x => x.SeriesId).NotEmpty();
}
