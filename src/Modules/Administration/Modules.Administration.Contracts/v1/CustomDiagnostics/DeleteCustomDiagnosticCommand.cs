using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;

public sealed record DeleteCustomDiagnosticCommand(Guid Id) : ICommand<Unit>;
