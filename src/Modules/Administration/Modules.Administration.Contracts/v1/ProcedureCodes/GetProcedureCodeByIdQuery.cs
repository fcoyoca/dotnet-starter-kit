using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ProcedureCodes;

public sealed record GetProcedureCodeByIdQuery(Guid Id) : IQuery<ProcedureCodeDto>;
