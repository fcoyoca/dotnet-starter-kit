using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ScheduleConfig;

/// <summary>
/// Gets the schedule units for a clinic. Returns sensible defaults (no persisted row yet) rather than 404,
/// so the dashboard can always render an editable form.
/// </summary>
public sealed record GetScheduleConfigQuery(Guid ClinicId) : IQuery<ScheduleConfigDto>;
