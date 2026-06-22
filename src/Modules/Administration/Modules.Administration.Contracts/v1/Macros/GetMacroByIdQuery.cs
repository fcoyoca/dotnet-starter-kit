using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Macros;

public sealed record GetMacroByIdQuery(Guid Id) : IQuery<MacroDto>;
