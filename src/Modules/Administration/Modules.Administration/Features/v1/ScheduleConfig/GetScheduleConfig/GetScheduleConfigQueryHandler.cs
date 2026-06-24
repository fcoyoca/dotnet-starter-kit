using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.ScheduleConfig;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ScheduleConfig.GetScheduleConfig;

public sealed class GetScheduleConfigQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetScheduleConfigQuery, ScheduleConfigDto>
{
    /// <summary>Defaults surfaced when a clinic has no persisted schedule units yet (8:00 AM–5:00 PM, 15-min slots).</summary>
    private static readonly TimeOnly DefaultStart = new(8, 0);
    private static readonly TimeOnly DefaultEnd = new(17, 0);
    private const int DefaultIntervalMinutes = 15;

    public async ValueTask<ScheduleConfigDto> Handle(GetScheduleConfigQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        Domain.ScheduleConfig? entity = await dbContext.ScheduleConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ClinicId == query.ClinicId, cancellationToken)
            .ConfigureAwait(false);

        return entity is null
            ? new ScheduleConfigDto(query.ClinicId, DefaultStart, DefaultEnd, DefaultIntervalMinutes)
            : new ScheduleConfigDto(entity.ClinicId, entity.StartTime, entity.EndTime, entity.IntervalMinutes);
    }
}
