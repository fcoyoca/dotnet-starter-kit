using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;

public sealed record GetCustomDiagnosticByIdQuery(Guid Id) : IQuery<CustomDiagnosticDto>;
