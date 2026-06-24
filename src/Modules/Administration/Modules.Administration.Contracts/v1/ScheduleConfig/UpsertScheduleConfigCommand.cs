using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ScheduleConfig;

/// <summary>Creates or updates the schedule units for a clinic (one row per clinic).</summary>
public sealed record UpsertScheduleConfigCommand(
    Guid ClinicId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int IntervalMinutes) : ICommand<ScheduleConfigDto>;
