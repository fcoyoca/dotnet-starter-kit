using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Diagnostics;

public sealed record DeleteDiagnosticCommand(int Id) : ICommand<Unit>;
