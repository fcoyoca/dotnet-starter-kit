using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Appointments;

/// <summary>Soft-deletes every reserve-time block in a recurring series. Returns the number of blocks deleted.</summary>
public sealed record DeleteReservationSeriesCommand(Guid SeriesId) : ICommand<int>;
