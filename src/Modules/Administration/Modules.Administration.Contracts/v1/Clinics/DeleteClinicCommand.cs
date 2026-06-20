using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Clinics;

public sealed record DeleteClinicCommand(Guid Id) : ICommand<Unit>;
