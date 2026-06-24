using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Appointments;

public sealed record GetAppointmentByIdQuery(Guid Id) : IQuery<AppointmentDto>;
