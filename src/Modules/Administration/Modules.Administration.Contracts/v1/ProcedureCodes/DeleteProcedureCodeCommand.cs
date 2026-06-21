using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ProcedureCodes;

public sealed record DeleteProcedureCodeCommand(Guid Id) : ICommand<Unit>;
