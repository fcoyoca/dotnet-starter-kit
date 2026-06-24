using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ScheduleConfig;

namespace FSH.Modules.Administration.Features.v1.ScheduleConfig.UpsertScheduleConfig;

public sealed class UpsertScheduleConfigCommandValidator : AbstractValidator<UpsertScheduleConfigCommand>
{
    public UpsertScheduleConfigCommandValidator()
    {
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.IntervalMinutes).InclusiveBetween(5, 240);
        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time.");
    }
}
