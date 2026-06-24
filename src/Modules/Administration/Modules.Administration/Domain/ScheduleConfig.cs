using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// Per-clinic schedule units (legacy <c>ScheduleConfig</c> — <c>scTimespanMin</c>/<c>scTimespanMax</c>/
/// <c>scTimespanInterval</c>, keyed by <c>scClinicID</c>): the start/end of the visible scheduler day and the
/// slot interval. Exactly one row per <see cref="Clinic"/>. Tenant-scoped (NOT <see cref="IGlobalEntity"/>) so
/// each tenant owns its clinics' schedule units. Legacy stored the times as HHMM integers (e.g. 630 = 06:30,
/// 2100 = 21:00); here they are modelled as <see cref="TimeOnly"/>.
/// </summary>
public sealed class ScheduleConfig : AggregateRoot<Guid>
{
    public Guid ClinicId { get; private set; }

    /// <summary>First slot of the scheduler day (legacy <c>scTimespanMin</c>).</summary>
    public TimeOnly StartTime { get; private set; }

    /// <summary>End of the scheduler day (legacy <c>scTimespanMax</c>).</summary>
    public TimeOnly EndTime { get; private set; }

    /// <summary>Slot length in minutes (legacy <c>scTimespanInterval</c>).</summary>
    public int IntervalMinutes { get; private set; }

    /// <summary>Legacy <c>scID</c> of the source record; null for native records.</summary>
    public int? LegacyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private ScheduleConfig() { }

    public static ScheduleConfig Create(
        Guid clinicId,
        TimeOnly startTime,
        TimeOnly endTime,
        int intervalMinutes,
        int? legacyId = null)
    {
        Guard(clinicId, startTime, endTime, intervalMinutes);
        return new ScheduleConfig
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            StartTime = startTime,
            EndTime = endTime,
            IntervalMinutes = intervalMinutes,
            LegacyId = legacyId,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(TimeOnly startTime, TimeOnly endTime, int intervalMinutes)
    {
        Guard(ClinicId, startTime, endTime, intervalMinutes);
        StartTime = startTime;
        EndTime = endTime;
        IntervalMinutes = intervalMinutes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void Guard(Guid clinicId, TimeOnly startTime, TimeOnly endTime, int intervalMinutes)
    {
        if (clinicId == Guid.Empty)
        {
            throw new ArgumentException("Clinic id is required.", nameof(clinicId));
        }

        if (endTime <= startTime)
        {
            throw new ArgumentException("End time must be after start time.", nameof(endTime));
        }

        if (intervalMinutes <= 0)
        {
            throw new ArgumentException("Interval must be greater than zero.", nameof(intervalMinutes));
        }
    }
}
