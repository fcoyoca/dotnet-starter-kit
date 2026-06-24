namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record ScheduleConfigDto(
    Guid ClinicId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int IntervalMinutes);
