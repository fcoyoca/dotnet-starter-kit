using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.AppointmentTypes;

public sealed record ListAppointmentTypesQuery(bool? IsActive = null) : IQuery<IReadOnlyList<AppointmentTypeDto>>;
