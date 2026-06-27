using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Diagnostics;

public sealed record GetDiagnosticByIdQuery(int Id) : IQuery<DiagnosticDto>;
